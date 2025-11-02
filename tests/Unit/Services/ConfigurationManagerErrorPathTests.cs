using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceWatcher.Models;
using ServiceWatcher.Services;
using Xunit;

namespace ServiceWatcher.Tests.Unit.Services;

public class ConfigurationManagerErrorPathTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sw_cfg_err_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task LoadAsync_CorruptPrimary_UsesBackupIfValid()
    {
        var dir = CreateTempDir();
        var primaryPath = Path.Combine(dir, "config.json");
        var backupPath = Path.Combine(dir, "config.backup.json");
        await File.WriteAllTextAsync(primaryPath, "{ invalid json");
        var validConfig = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new() { new MonitoredService { ServiceName = "svc1", DisplayName = "Service 1", NotificationEnabled = true, IsAvailable = true } }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(validConfig, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(backupPath, json);

        var logger = new Mock<ILogger<ConfigurationManager>>();
        var mgr = new ConfigurationManager(logger.Object, primaryPath);
        var result = await mgr.LoadAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.MonitoringIntervalSeconds);
    }

    [Fact]
    public async Task LoadAsync_CorruptPrimary_NoBackup_CreatesDefault()
    {
        var dir = CreateTempDir();
        var primaryPath = Path.Combine(dir, "config.json");
        await File.WriteAllTextAsync(primaryPath, "{ invalid json");
        var logger = new Mock<ILogger<ConfigurationManager>>();
        var mgr = new ConfigurationManager(logger.Object, primaryPath);
        var result = await mgr.LoadAsync();
        Assert.True(result.IsSuccess);
        // Default contains Windows Update service
        Assert.Contains(result.Value.MonitoredServices, s => s.ServiceName == "wuauserv");
    }

    [Fact]
    public async Task RestoreFromBackupAsync_InvalidBackup_Fails()
    {
        var dir = CreateTempDir();
        var primaryPath = Path.Combine(dir, "config.json");
        var logger = new Mock<ILogger<ConfigurationManager>>();
        var mgr = new ConfigurationManager(logger.Object, primaryPath);
        // Create invalid backup
        var backupPath = Path.Combine(dir, "config.backup.json");
        await File.WriteAllTextAsync(backupPath, "{ invalid json");
        var restore = await mgr.RestoreFromBackupAsync();
        Assert.False(restore.IsSuccess);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_ValidBackup_Succeeds()
    {
        var dir = CreateTempDir();
        var primaryPath = Path.Combine(dir, "config.json");
        var logger = new Mock<ILogger<ConfigurationManager>>();
        var mgr = new ConfigurationManager(logger.Object, primaryPath);

        // First save a valid config to create backup
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new() { new MonitoredService { ServiceName = "svcX", DisplayName = "Service X", NotificationEnabled = true, IsAvailable = true } }
        };
        var save1 = await mgr.SaveAsync(config);
        Assert.True(save1.IsSuccess);

        // Modify current config and save again so backup has previous state
        config.MonitoringIntervalSeconds = 10;
        var save2 = await mgr.SaveAsync(config);
        Assert.True(save2.IsSuccess);

        // Now restore from backup (which holds interval=5)
        var restore = await mgr.RestoreFromBackupAsync();
        Assert.True(restore.IsSuccess);

        var reloaded = await mgr.LoadAsync();
        Assert.True(reloaded.IsSuccess);
    // Backupは interval=5 を保持し restore後に現在値が5へ戻ることを期待
    Assert.Equal(5, reloaded.Value.MonitoringIntervalSeconds);
    }
}
