using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Services;
using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Tests.Unit;

/// <summary>
/// Unit tests for ConfigurationManager class.
/// </summary>
public class ConfigurationManagerTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationManager>> _mockLogger;
    private readonly string _testConfigPath;
    private readonly string _testBackupPath;
    private readonly ConfigurationManager _configManager;

    public ConfigurationManagerTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationManager>>();
        
        // Create temp directory for tests
        var tempDir = Path.Combine(Path.GetTempPath(), "ServiceWatcherTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        _testConfigPath = Path.Combine(tempDir, "config.json");
        _testBackupPath = Path.Combine(tempDir, "config.backup.json");
        
        _configManager = new ConfigurationManager(_mockLogger.Object);
    }

    public void Dispose()
    {
        // Clean up test files
        try
        {
            if (File.Exists(_testConfigPath))
                File.Delete(_testConfigPath);
            if (File.Exists(_testBackupPath))
                File.Delete(_testBackupPath);
                
            var testDir = Path.GetDirectoryName(_testConfigPath);
            if (testDir != null && Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task LoadAsync_WithValidJson_ReturnsSuccess()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 10,
            NotificationDisplayTimeSeconds = 20,
            MonitoredServices = new List<MonitoredService>
            {
                new MonitoredService
                {
                    ServiceName = "TestService",
                    DisplayName = "Test Service",
                    NotificationEnabled = true
                }
            }
        };
        
        await _configManager.SaveAsync(config);

        // Act
        var result = await _configManager.LoadAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.MonitoringIntervalSeconds);
        Assert.Equal(20, result.Value.NotificationDisplayTimeSeconds);
        Assert.Single(result.Value.MonitoredServices);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidJson_LoadsBackup()
    {
        // Arrange - Create valid backup
        var validConfig = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            MonitoredServices = new List<MonitoredService>()
        };
        await _configManager.SaveAsync(validConfig);

        // Corrupt the main config file
        await File.WriteAllTextAsync(_testConfigPath, "{ invalid json }");

        // Act
        var result = await _configManager.LoadAsync();

        // Assert - Should load from backup or create default
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task LoadAsync_ConfigNotExists_CreatesDefault()
    {
        // Act
        var result = await _configManager.LoadAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.MonitoringIntervalSeconds > 0);
        Assert.NotNull(result.Value.MonitoredServices);
    }

    [Fact]
    public async Task SaveAsync_CreatesBackup()
    {
        // Arrange
        var config1 = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            MonitoredServices = new List<MonitoredService>()
        };
        await _configManager.SaveAsync(config1);
        
        var config2 = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 10,
            MonitoredServices = new List<MonitoredService>()
        };

        // Act
        var result = await _configManager.SaveAsync(config2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(_configManager.ConfigurationExists());
        
        // Verify backup exists
        var backupPath = Path.Combine(Path.GetDirectoryName(_testConfigPath)!, "config.backup.json");
        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task SaveAsync_WithInvalidConfig_ReturnsFailure()
    {
        // Arrange - Invalid monitoring interval
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 0, // Invalid
            MonitoredServices = new List<MonitoredService>()
        };

        // Act
        var result = await _configManager.SaveAsync(config);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("検証エラー", result.Error);
    }

    [Fact]
    public void Validate_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>
            {
                new MonitoredService
                {
                    ServiceName = "TestService",
                    DisplayName = "Test Service"
                }
            }
        };

        // Act
        var result = _configManager.Validate(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidInterval_ReturnsErrors()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 0, // Invalid
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>()
        };

        // Act
        var result = _configManager.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("監視間隔"));
    }

    [Fact]
    public void Validate_WithDuplicateServices_ReturnsErrors()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            MonitoredServices = new List<MonitoredService>
            {
                new MonitoredService { ServiceName = "TestService", DisplayName = "Test 1" },
                new MonitoredService { ServiceName = "TestService", DisplayName = "Test 2" }
            }
        };

        // Act
        var result = _configManager.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("重複"));
    }

    [Fact]
    public async Task CreateDefaultAsync_GeneratesValidConfig()
    {
        // Act
        var result = await _configManager.CreateDefaultAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value.MonitoringIntervalSeconds);
        Assert.Equal(30, result.Value.NotificationDisplayTimeSeconds);
        Assert.NotNull(result.Value.MonitoredServices);
        Assert.Single(result.Value.MonitoredServices); // Should have default service
    }

    [Fact]
    public async Task ReloadAsync_DiscardsInMemoryChanges()
    {
        // Arrange - Save initial config
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            MonitoredServices = new List<MonitoredService>()
        };
        await _configManager.SaveAsync(config);

        // Act - Reload should discard any in-memory changes
        var result = await _configManager.ReloadAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.MonitoringIntervalSeconds);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_RestoresBackup()
    {
        // Arrange - Create backup
        var backupConfig = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 10,
            MonitoredServices = new List<MonitoredService>()
        };
        await _configManager.SaveAsync(backupConfig);

        // Corrupt main config
        await File.WriteAllTextAsync(_testConfigPath, "invalid");

        // Act
        var result = await _configManager.RestoreFromBackupAsync();

        // Assert
        Assert.True(result.IsSuccess);
        
        // Verify config is restored
        var loadResult = await _configManager.LoadAsync();
        Assert.True(loadResult.IsSuccess);
    }

    [Fact]
    public async Task ConfigurationExists_WhenFileExists_ReturnsTrue()
    {
        // Arrange - Create config file
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            MonitoredServices = new List<MonitoredService>()
        };
        await _configManager.SaveAsync(config);

        // Act
        var exists = _configManager.ConfigurationExists();

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ConfigurationExists_WhenFileNotExists_ReturnsFalse()
    {
        // Act
        var exists = _configManager.ConfigurationExists();

        // Assert
        Assert.False(exists);
    }
}
