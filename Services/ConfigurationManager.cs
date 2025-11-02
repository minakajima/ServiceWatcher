using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Manages application configuration with JSON persistence, validation, and backup.
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly string _configFilePath;
    private readonly string _backupFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConfigurationValidator _validator;
    private ApplicationConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of ConfigurationManager.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ConfigurationManager(ILogger<ConfigurationManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Set file paths in %LocalAppData%/ServiceWatcher
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
        
        _logger.LogInformation("ConfigurationManager initialized. Config path: {Path}", _configFilePath);
    }

    /// <inheritdoc />
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading configuration");
            return Result<ApplicationConfiguration>.Failure($"予期しないエラー: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> SaveAsync(ApplicationConfiguration configuration)
    {
        try
        {
            _logger.LogInformation("Saving configuration to {Path}", _configFilePath);
            
            // Validate
            var validation = _validator.Validate(configuration);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors);
                _logger.LogError("Configuration validation failed: {Errors}", errors);
                return Result<bool>.Failure($"検証エラー: {errors}");
            }
            
            // Backup existing configuration
            if (File.Exists(_configFilePath))
            {
                File.Copy(_configFilePath, _backupFilePath, overwrite: true);
                _logger.LogDebug("Created backup at {BackupPath}", _backupFilePath);
            }
            
            // Serialize to JSON
            var json = JsonSerializer.Serialize(configuration, _jsonOptions);
            
            // Write to file
            await File.WriteAllTextAsync(_configFilePath, json);
            
            _logger.LogInformation("Configuration saved successfully");
            
            // Update in-memory configuration and raise event
            var oldConfig = _configuration;
            _configuration = configuration;
            
            ConfigurationChanged?.Invoke(this, configuration);
            
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

    /// <inheritdoc />
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
            StartMinimized = false,
            AutoStartMonitoring = false
        };
        
        // Save default configuration
        var saveResult = await SaveAsync(defaultConfig);
        
        if (!saveResult.IsSuccess)
        {
            return Result<ApplicationConfiguration>.Failure(saveResult.Error!);
        }
        
        _configuration = defaultConfig;
        return Result<ApplicationConfiguration>.Success(defaultConfig);
    }

    /// <inheritdoc />
    public ValidationResult Validate(ApplicationConfiguration configuration)
    {
        return _validator.Validate(configuration);
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationConfiguration>> TryLoadBackupAsync()
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
                var validation = _validator.Validate(config);
                if (validation.IsValid)
                {
                    _configuration = config;
                    _logger.LogInformation("Loaded configuration from backup");
                    return Result<ApplicationConfiguration>.Success(config);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load backup configuration");
        }
        
        // Both failed, create default
        return await CreateDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Result<ApplicationConfiguration>> ReloadAsync()
    {
        _logger.LogInformation("Reloading configuration from disk");
        return await LoadAsync();
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RestoreFromBackupAsync()
    {
        try
        {
            _logger.LogInformation("Restoring configuration from backup");
            
            if (!File.Exists(_backupFilePath))
            {
                return Result<bool>.Failure("バックアップファイルが見つかりません");
            }
            
            // Load and validate backup
            var json = await File.ReadAllTextAsync(_backupFilePath);
            var config = JsonSerializer.Deserialize<ApplicationConfiguration>(json, _jsonOptions);
            
            if (config == null)
            {
                return Result<bool>.Failure("バックアップファイルの解析に失敗しました");
            }
            
            var validation = _validator.Validate(config);
            if (!validation.IsValid)
            {
                return Result<bool>.Failure($"バックアップの検証に失敗: {string.Join(", ", validation.Errors)}");
            }
            
            // Restore by saving backup as current config
            File.Copy(_backupFilePath, _configFilePath, overwrite: true);
            _configuration = config;
            
            _logger.LogInformation("Configuration restored from backup");
            ConfigurationChanged?.Invoke(this, config);
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from backup");
            return Result<bool>.Failure($"バックアップからの復元に失敗: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool ConfigurationExists()
    {
        return File.Exists(_configFilePath);
    }

    /// <inheritdoc />
    public event EventHandler<ApplicationConfiguration>? ConfigurationChanged;
}
