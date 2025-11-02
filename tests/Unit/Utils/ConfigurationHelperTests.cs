using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceWatcher.Utils;
using Xunit;

namespace ServiceWatcher.Tests.Unit.Utils;

public class ConfigurationHelperTests
{
    private string NewTempPath(string fileName = "config.json")
    {
        var dir = Path.Combine(Path.GetTempPath(), "sw_cfg_helper_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, fileName);
    }

    [Fact]
    public void EnsureConfigDirectory_CreatesDirectory()
    {
        var path = NewTempPath();
        var logger = new Mock<ILogger>();
        // Delete directory to force creation
        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        var result = ConfigurationHelper.EnsureConfigDirectory(path, logger.Object);
        Assert.True(result.IsSuccess);
        Assert.True(Directory.Exists(result.Value));
    }

    [Fact]
    public void EnsureConfigDirectory_InvalidPath_Fails()
    {
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.EnsureConfigDirectory("  ", logger.Object);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsConfigWritable_NewFile_Succeeds()
    {
        var path = NewTempPath();
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.IsConfigWritable(path, logger.Object);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IsConfigWritable_ReadOnlyFile_Fails()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{}");
        var fileInfo = new FileInfo(path) { IsReadOnly = true };
        fileInfo.IsReadOnly = true; // ensure readonly
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.IsConfigWritable(path, logger.Object);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CreateBackup_NoSource_Fails()
    {
        var path = NewTempPath();
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.CreateBackup(path, logger.Object);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CreateBackup_Valid_Succeeds()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{\"monitoringIntervalSeconds\":5,\"monitoredServices\":[]}");
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.CreateBackup(path, logger.Object);
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.Value));
    }

    [Fact]
    public void ValidateAndRepairConfig_InvalidJson_WithBackup_ReturnsFailureSuggestingBackup()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{ invalid json");
        File.WriteAllText(path + ".bak", "{\"monitoringIntervalSeconds\":5,\"monitoredServices\":[]}");
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.ValidateAndRepairConfig(path, logger.Object);
        Assert.False(result.IsSuccess);
        Assert.Contains("バックアップファイルが見つかりました", result.Error);
    }

    [Fact]
    public void ValidateAndRepairConfig_MissingRequiredProperties_Fails()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{\"configurationVersion\":\"1.0\"}");
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.ValidateAndRepairConfig(path, logger.Object);
        Assert.False(result.IsSuccess);
        Assert.Contains("必要なプロパティ", result.Error);
    }

    [Fact]
    public void ValidateAndRepairConfig_Valid_Succeeds()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{\"monitoringIntervalSeconds\":5,\"monitoredServices\":[]}");
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.ValidateAndRepairConfig(path, logger.Object);
        Assert.True(result.IsSuccess);
    }
}
