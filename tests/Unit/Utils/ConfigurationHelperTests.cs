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
        Assert.Contains("missing required properties", result.Error);
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

    [Fact]
    public void ValidateAndRepairConfig_FileNotExists_Fails()
    {
        var path = NewTempPath();
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.ValidateAndRepairConfig(path, logger.Object);
        Assert.False(result.IsSuccess);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public void ValidateAndRepairConfig_InvalidJson_NoBackup_Fails()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{ not valid json at all");
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.ValidateAndRepairConfig(path, logger.Object);
        Assert.False(result.IsSuccess);
        Assert.Contains("破損", result.Error);
        Assert.Contains("バックアップも見つかりませんでした", result.Error);
    }

    [Fact]
    public void RestoreFromBackup_NoBackup_Fails()
    {
        var path = NewTempPath();
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.RestoreFromBackup(path, logger.Object);
        Assert.False(result.IsSuccess);
        Assert.Contains("見つかりません", result.Error);
    }

    [Fact]
    public void RestoreFromBackup_CorruptedBackup_Fails()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{ corrupted }");
        File.WriteAllText(path + ".bak", "{ also corrupted }");
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.RestoreFromBackup(path, logger.Object);
        Assert.False(result.IsSuccess);
        Assert.Contains("バックアップファイルも破損", result.Error);
    }

    [Fact]
    public void RestoreFromBackup_ValidBackup_Succeeds()
    {
        var path = NewTempPath();
        File.WriteAllText(path, "{ corrupted current }");
        File.WriteAllText(path + ".bak", "{\"monitoringIntervalSeconds\":10,\"monitoredServices\":[]}");
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.RestoreFromBackup(path, logger.Object);
        Assert.True(result.IsSuccess);
        var restored = File.ReadAllText(path);
        Assert.Contains("monitoringIntervalSeconds", restored);
        Assert.True(File.Exists(path + ".corrupted")); // Original saved
    }

    [Fact]
    public void CreateDefaultConfig_Succeeds()
    {
        var path = NewTempPath();
        var logger = new Mock<ILogger>();
        var result = ConfigurationHelper.CreateDefaultConfig(path, logger.Object);
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        Assert.Contains("monitoringIntervalSeconds", content);
        Assert.Contains("monitoredServices", content);
    }

    [Fact]
    public void GetUserFriendlyErrorMessage_UnauthorizedAccess_ReturnsCorrectMessage()
    {
        var ex = new UnauthorizedAccessException("test");
        var message = ConfigurationHelper.GetUserFriendlyErrorMessage(ex);
        Assert.Contains("アクセス権限", message);
    }

    [Fact]
    public void GetUserFriendlyErrorMessage_JsonException_ReturnsCorrectMessage()
    {
        var ex = new System.Text.Json.JsonException("test");
        var message = ConfigurationHelper.GetUserFriendlyErrorMessage(ex);
        Assert.Contains("形式が正しくありません", message);
    }

    [Fact]
    public void GetUserFriendlyErrorMessage_FileNotFound_ReturnsCorrectMessage()
    {
        var ex = new FileNotFoundException("test");
        var message = ConfigurationHelper.GetUserFriendlyErrorMessage(ex);
        Assert.Contains("見つかりません", message);
    }

    [Fact]
    public void GetUserFriendlyErrorMessage_IOException_ReturnsCorrectMessage()
    {
        var ex = new IOException("test");
        var message = ConfigurationHelper.GetUserFriendlyErrorMessage(ex);
        Assert.Contains("読み書きに失敗", message);
    }

    [Fact]
    public void GetUserFriendlyErrorMessage_Generic_ReturnsGenericMessage()
    {
        var ex = new InvalidOperationException("custom error");
        var message = ConfigurationHelper.GetUserFriendlyErrorMessage(ex);
        Assert.Contains("予期しないエラー", message);
        Assert.Contains("custom error", message);
    }
}
