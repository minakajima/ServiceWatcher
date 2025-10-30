# IConfigurationManager Contract

**Version**: 1.0  
**Status**: Draft  
**Last Updated**: 2025-10-30

## Purpose

Defines the interface for loading, saving, and managing application configuration. Handles JSON serialization, validation, and file I/O for `ApplicationConfiguration`.

## Interface Definition

```csharp
namespace ServiceWatcher.Services
{
    /// <summary>
    /// Interface for managing application configuration.
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Gets the currently loaded configuration.
        /// </summary>
        ApplicationConfiguration Configuration { get; }
        
        /// <summary>
        /// Event raised when configuration changes.
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
        
        /// <summary>
        /// Loads configuration from disk. Creates default if not found.
        /// </summary>
        /// <returns>Result with loaded configuration.</returns>
        Task<Result<ApplicationConfiguration>> LoadAsync();
        
        /// <summary>
        /// Saves configuration to disk with backup.
        /// </summary>
        /// <param name="configuration">Configuration to save.</param>
        /// <param name="validateFirst">Whether to validate before saving.</param>
        /// <returns>Result indicating success or failure.</returns>
        Task<Result<bool>> SaveAsync(ApplicationConfiguration configuration, bool validateFirst = true);
        
        /// <summary>
        /// Validates configuration without saving.
        /// </summary>
        /// <param name="configuration">Configuration to validate.</param>
        /// <returns>Validation result with errors if any.</returns>
        ValidationResult Validate(ApplicationConfiguration configuration);
        
        /// <summary>
        /// Reloads configuration from disk, discarding in-memory changes.
        /// </summary>
        /// <returns>Result with reloaded configuration.</returns>
        Task<Result<ApplicationConfiguration>> ReloadAsync();
        
        /// <summary>
        /// Restores configuration from backup file.
        /// </summary>
        /// <returns>Result indicating success or failure.</returns>
        Task<Result<bool>> RestoreFromBackupAsync();
        
        /// <summary>
        /// Gets the configuration file path.
        /// </summary>
        string GetConfigurationFilePath();
        
        /// <summary>
        /// Checks if configuration file exists.
        /// </summary>
        bool ConfigurationExists();
        
        /// <summary>
        /// Creates default configuration file.
        /// </summary>
        /// <returns>Result with created configuration.</returns>
        Task<Result<ApplicationConfiguration>> CreateDefaultAsync();
    }
    
    /// <summary>
    /// Event args for configuration changes.
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public ApplicationConfiguration OldConfiguration { get; }
        public ApplicationConfiguration NewConfiguration { get; }
        public DateTime Timestamp { get; }
        
        public ConfigurationChangedEventArgs(
            ApplicationConfiguration oldConfig,
            ApplicationConfiguration newConfig)
        {
            OldConfiguration = oldConfig;
            NewConfiguration = newConfig;
            Timestamp = DateTime.Now;
        }
    }
}
```

## Usage Example

### Loading Configuration

```csharp
public class Program
{
    private static async Task Main()
    {
        var logger = CreateLogger();
        var configManager = new ConfigurationManager(logger);
        
        // Load configuration (creates default if not found)
        var result = await configManager.LoadAsync();
        
        if (!result.IsSuccess)
        {
            logger.LogError("Failed to load configuration: {Error}", result.ErrorMessage);
            return;
        }
        
        var config = result.Value;
        logger.LogInformation("Loaded {Count} monitored services", 
            config.MonitoredServices.Count);
        
        // Use configuration
        var serviceMonitor = new ServiceMonitor(logger, configManager);
        // ...
    }
}
```

### Saving Configuration

```csharp
private async void SaveButton_Click(object sender, EventArgs e)
{
    // Get configuration from UI
    var config = new ApplicationConfiguration
    {
        MonitoringIntervalSeconds = (int)IntervalNumericUpDown.Value,
        NotificationDisplayTimeSeconds = (int)NotificationTimeNumericUpDown.Value,
        MonitoredServices = GetMonitoredServicesFromGrid(),
        StartMinimized = StartMinimizedCheckBox.Checked,
        AutoStartMonitoring = AutoStartCheckBox.Checked
    };
    
    // Validate first
    var validation = _configManager.Validate(config);
    if (!validation.IsValid)
    {
        var errors = string.Join("\n", validation.Errors);
        MessageBox.Show($"設定エラー:\n{errors}", "検証失敗", 
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }
    
    // Save
    var result = await _configManager.SaveAsync(config);
    
    if (result.IsSuccess)
    {
        MessageBox.Show("設定を保存しました。", "成功");
    }
    else
    {
        MessageBox.Show($"保存失敗: {result.ErrorMessage}", "エラー");
    }
}
```

### Handling Configuration Changes

```csharp
public class MainForm : Form
{
    private readonly IConfigurationManager _configManager;
    
    public MainForm(IConfigurationManager configManager)
    {
        _configManager = configManager;
        
        // Subscribe to configuration changes
        _configManager.ConfigurationChanged += OnConfigurationChanged;
    }
    
    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        var newConfig = e.NewConfiguration;
        
        // Update monitoring interval if changed
        if (newConfig.MonitoringIntervalSeconds != e.OldConfiguration.MonitoringIntervalSeconds)
        {
            _logger.LogInformation("Monitoring interval changed to {Interval}s", 
                newConfig.MonitoringIntervalSeconds);
            
            // Notify ServiceMonitor to update interval
            _serviceMonitor.UpdateMonitoringIntervalAsync(newConfig.MonitoringIntervalSeconds);
        }
        
        // Refresh UI
        RefreshConfigurationUI();
    }
}
```

## Implementation Requirements

### Constructor

```csharp
public class ConfigurationManager : IConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly string _configFilePath;
    private readonly string _backupFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConfigurationValidator _validator;
    private ApplicationConfiguration _configuration;
    
    public ConfigurationManager(ILogger<ConfigurationManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Set file paths
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ServiceWatcher");
        Directory.CreateDirectory(appFolder);
        
        _configFilePath = Path.Combine(appFolder, "config.json");
        _backupFilePath = Path.Combine(appFolder, "config.backup.json");
        
        // Configure JSON serialization
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        
        _validator = new ConfigurationValidator();
    }
}
```

### Load Implementation

```csharp
public async Task<Result<ApplicationConfiguration>> LoadAsync()
{
    try
    {
        _logger.LogInformation("Loading configuration from {Path}", _configFilePath);
        
        // Check if file exists
        if (!File.Exists(_configFilePath))
        {
            _logger.LogInformation("Configuration file not found, creating default");
            return await CreateDefaultAsync();
        }
        
        // Read and parse JSON
        var json = await File.ReadAllTextAsync(_configFilePath);
        var config = JsonSerializer.Deserialize<ApplicationConfiguration>(json, _jsonOptions);
        
        if (config == null)
        {
            _logger.LogError("Failed to deserialize configuration");
            return await TryLoadBackupAsync();
        }
        
        // Validate
        var validation = _validator.Validate(config);
        if (!validation.IsValid)
        {
            _logger.LogError("Configuration validation failed: {Errors}", 
                string.Join(", ", validation.Errors));
            return await TryLoadBackupAsync();
        }
        
        _configuration = config;
        _logger.LogInformation("Configuration loaded successfully");
        
        return Result<ApplicationConfiguration>.Success(config);
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Failed to parse configuration JSON");
        return await TryLoadBackupAsync();
    }
    catch (IOException ex)
    {
        _logger.LogError(ex, "Failed to read configuration file");
        return Result<ApplicationConfiguration>.Failure($"ファイル読み込みエラー: {ex.Message}");
    }
}

private async Task<Result<ApplicationConfiguration>> TryLoadBackupAsync()
{
    _logger.LogInformation("Attempting to load backup configuration");
    
    if (!File.Exists(_backupFilePath))
    {
        _logger.LogWarning("Backup file not found, creating default");
        return await CreateDefaultAsync();
    }
    
    try
    {
        var json = await File.ReadAllTextAsync(_backupFilePath);
        var config = JsonSerializer.Deserialize<ApplicationConfiguration>(json, _jsonOptions);
        
        if (config != null)
        {
            _configuration = config;
            _logger.LogInformation("Loaded configuration from backup");
            return Result<ApplicationConfiguration>.Success(config);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load backup configuration");
    }
    
    // Both failed, create default
    return await CreateDefaultAsync();
}
```

### Save Implementation

```csharp
public async Task<Result<bool>> SaveAsync(ApplicationConfiguration configuration, bool validateFirst = true)
{
    try
    {
        _logger.LogInformation("Saving configuration to {Path}", _configFilePath);
        
        // Validate if requested
        if (validateFirst)
        {
            var validation = _validator.Validate(configuration);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors);
                _logger.LogError("Configuration validation failed: {Errors}", errors);
                return Result<bool>.Failure($"検証エラー: {errors}");
            }
        }
        
        // Backup existing configuration
        if (File.Exists(_configFilePath))
        {
            File.Copy(_configFilePath, _backupFilePath, overwrite: true);
            _logger.LogDebug("Created backup at {BackupPath}", _backupFilePath);
        }
        
        // Update LastModified
        configuration.LastModified = DateTime.Now;
        
        // Serialize to JSON
        var json = JsonSerializer.Serialize(configuration, _jsonOptions);
        
        // Write to file
        await File.WriteAllTextAsync(_configFilePath, json);
        
        _logger.LogInformation("Configuration saved successfully");
        
        // Update in-memory configuration and raise event
        var oldConfig = _configuration;
        _configuration = configuration;
        
        ConfigurationChanged?.Invoke(this, 
            new ConfigurationChangedEventArgs(oldConfig, configuration));
        
        return Result<bool>.Success(true);
    }
    catch (IOException ex)
    {
        _logger.LogError(ex, "Failed to write configuration file");
        return Result<bool>.Failure($"ファイル書き込みエラー: {ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error saving configuration");
        return Result<bool>.Failure($"予期しないエラー: {ex.Message}");
    }
}
```

### Create Default Implementation

```csharp
public async Task<Result<ApplicationConfiguration>> CreateDefaultAsync()
{
    _logger.LogInformation("Creating default configuration");
    
    var defaultConfig = new ApplicationConfiguration
    {
        MonitoringIntervalSeconds = 5,
        NotificationDisplayTimeSeconds = 30,
        MonitoredServices = new List<MonitoredService>
        {
            // Example: Windows Update service
            new MonitoredService
            {
                ServiceName = "wuauserv",
                DisplayName = "Windows Update",
                NotificationEnabled = true,
                LastKnownStatus = ServiceStatus.Unknown,
                IsAvailable = true
            }
        },
        ConfigurationVersion = "1.0",
        LastModified = DateTime.Now,
        StartMinimized = false,
        AutoStartMonitoring = false
    };
    
    // Save default configuration
    var saveResult = await SaveAsync(defaultConfig, validateFirst: false);
    
    if (!saveResult.IsSuccess)
    {
        return Result<ApplicationConfiguration>.Failure(saveResult.ErrorMessage!);
    }
    
    _configuration = defaultConfig;
    return Result<ApplicationConfiguration>.Success(defaultConfig);
}
```

## Error Handling

### Expected Exceptions

| Exception | Scenario | Handling |
|-----------|----------|----------|
| `JsonException` | Invalid JSON format | Load backup or create default |
| `IOException` | File access error | Return error, don't overwrite good config |
| `UnauthorizedAccessException` | Permission denied | Return error with suggestion to run as admin |
| `ValidationException` | Invalid configuration | Return validation errors |

## Performance Requirements

| Operation | Max Duration | Notes |
|-----------|--------------|-------|
| LoadAsync | <50ms | For 50 services |
| SaveAsync | <100ms | Includes backup creation |
| Validate | <10ms | For 50 services |
| CreateDefaultAsync | <100ms | Includes save operation |

## File Format

### Configuration File Location

- **Path**: `%LOCALAPPDATA%\ServiceWatcher\config.json`
- **Backup**: `%LOCALAPPDATA%\ServiceWatcher\config.backup.json`
- **Encoding**: UTF-8
- **Format**: JSON with indentation

### Example Configuration

See `data-model.md` for full JSON schema example.

## Thread Safety

- All public methods are thread-safe
- File I/O protected by async/await pattern
- Configuration property has lock for read/write

## Testing Checklist

- [ ] Load existing configuration
- [ ] Create default configuration
- [ ] Save and reload (round-trip)
- [ ] Validate valid configuration
- [ ] Validate invalid configuration (each validation rule)
- [ ] Handle corrupted JSON
- [ ] Handle missing file
- [ ] Restore from backup
- [ ] Handle file permission errors
- [ ] Configuration change event
- [ ] Concurrent save operations
- [ ] Large configuration (50 services)

## Dependencies

- `Microsoft.Extensions.Logging.ILogger<ConfigurationManager>`
- `System.Text.Json`
- `System.IO`
