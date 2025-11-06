using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ServiceWatcher.Utils;

/// <summary>
/// Configuration file error handling and recovery helper.
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Ensures the configuration directory exists.
    /// </summary>
    public static Result<string> EnsureConfigDirectory(string configPath, ILogger? logger = null)
    {
        try
        {
            var directory = Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(directory))
            {
                return Result<string>.Failure("Configuration file directory path is invalid.");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                logger?.LogInformation($"Created configuration directory: {directory}");
            }

            return Result<string>.Success(directory);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.LogError(ex, "Access denied when creating configuration directory");
            return Result<string>.Failure($"Permission denied to create directory: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to ensure configuration directory exists");
            return Result<string>.Failure($"Failed to create directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the configuration file is writable.
    /// </summary>
    public static Result<bool> IsConfigWritable(string configPath, ILogger? logger = null)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                // File doesn't exist, check if we can create it
                var dirResult = EnsureConfigDirectory(configPath, logger);
                if (!dirResult.IsSuccess)
                {
                    return Result<bool>.Failure(dirResult.Error!);
                }

                return Result<bool>.Success(true);
            }

            // Check if file is read-only
            var fileInfo = new FileInfo(configPath);
            if (fileInfo.IsReadOnly)
            {
                logger?.LogWarning($"Configuration file is read-only: {configPath}");
                return Result<bool>.Failure(
                    $"Configuration file is read-only.\n\n" +
                    $"File: {configPath}\n\n" +
                    $"Please uncheck 'Read-only' in the file properties.");
            }

            // Try to open for writing
            using (var stream = File.Open(configPath, FileMode.Open, FileAccess.Write))
            {
                // Successfully opened for writing
            }

            return Result<bool>.Success(true);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.LogError(ex, "Access denied when checking config file write access");
            return Result<bool>.Failure(
                $"Access denied to configuration file.\n\n" +
                $"File: {configPath}\n\n" +
                $"Please run as administrator or check file permissions.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to check config file write access");
            return Result<bool>.Failure($"Failed to check configuration file access: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates and repairs corrupted configuration file.
    /// </summary>
    public static Result<bool> ValidateAndRepairConfig(string configPath, ILogger? logger = null)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                logger?.LogWarning($"Configuration file does not exist: {configPath}");
                return Result<bool>.Failure("Configuration file does not exist.");
            }

            var json = File.ReadAllText(configPath);

            // Try to parse JSON
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check for required properties
                bool hasMonitoringInterval = root.TryGetProperty("monitoringIntervalSeconds", out _);
                bool hasServices = root.TryGetProperty("monitoredServices", out _);

                if (!hasMonitoringInterval || !hasServices)
                {
                    logger?.LogWarning("Configuration file is missing required properties");
                    return Result<bool>.Failure(
                        "Configuration file is missing required properties.\n\n" +
                        "Required: monitoringIntervalSeconds, monitoredServices");
                }

                logger?.LogInformation("Configuration file validation passed");
                return Result<bool>.Success(true);
            }
            catch (JsonException ex)
            {
                logger?.LogError(ex, "Configuration file has invalid JSON format");
                
                // Check for backup
                var backupPath = configPath + ".bak";
                if (File.Exists(backupPath))
                {
                    return Result<bool>.Failure(
                        $"設定ファイルが破損しています。\n\n" +
                        $"JSONの解析に失敗しました: {ex.Message}\n\n" +
                        $"バックアップファイルが見つかりました:\n{backupPath}\n\n" +
                        $"バックアップから復元しますか？");
                }
                else
                {
                    return Result<bool>.Failure(
                        $"設定ファイルが破損しており、バックアップも見つかりませんでした。\n\n" +
                        $"JSONの解析に失敗: {ex.Message}\n\n" +
                        $"設定ファイルを手動で修正するか、削除して再作成してください。");
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to validate configuration file");
            return Result<bool>.Failure($"設定ファイルの検証に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a backup of the configuration file.
    /// </summary>
    public static Result<string> CreateBackup(string configPath, ILogger? logger = null)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                return Result<string>.Failure("バックアップ元のファイルが存在しません。");
            }

            var backupPath = configPath + ".bak";
            File.Copy(configPath, backupPath, overwrite: true);

            logger?.LogInformation($"Created configuration backup: {backupPath}");
            return Result<string>.Success(backupPath);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create configuration backup");
            return Result<string>.Failure($"バックアップの作成に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores configuration from backup.
    /// </summary>
    public static Result<bool> RestoreFromBackup(string configPath, ILogger? logger = null)
    {
        try
        {
            var backupPath = configPath + ".bak";
            if (!File.Exists(backupPath))
            {
                return Result<bool>.Failure("バックアップファイルが見つかりません。");
            }

            // Validate backup before restoring
            var validateResult = ValidateAndRepairConfig(backupPath, logger);
            if (!validateResult.IsSuccess)
            {
                return Result<bool>.Failure($"バックアップファイルも破損しています: {validateResult.Error}");
            }

            // Create backup of current file (even if corrupted)
            if (File.Exists(configPath))
            {
                var corruptedBackup = configPath + ".corrupted";
                File.Copy(configPath, corruptedBackup, overwrite: true);
                logger?.LogInformation($"Saved corrupted file to: {corruptedBackup}");
            }

            // Restore from backup
            File.Copy(backupPath, configPath, overwrite: true);
            logger?.LogInformation($"Restored configuration from backup: {backupPath}");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to restore from backup");
            return Result<bool>.Failure($"バックアップからの復元に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a default configuration file.
    /// </summary>
    public static Result<bool> CreateDefaultConfig(string configPath, ILogger? logger = null)
    {
        try
        {
            var dirResult = EnsureConfigDirectory(configPath, logger);
            if (!dirResult.IsSuccess)
            {
                return Result<bool>.Failure(dirResult.Error!);
            }

            var defaultConfig = new
            {
                configurationVersion = "1.0",
                lastModified = DateTime.Now,
                monitoringIntervalSeconds = 5,
                notificationDisplayTimeSeconds = 30,
                startMinimized = false,
                autoStartMonitoring = false,
                monitoredServices = new object[] { }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(configPath, json);

            logger?.LogInformation($"Created default configuration file: {configPath}");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create default configuration");
            return Result<bool>.Failure($"デフォルト設定ファイルの作成に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a user-friendly error message for configuration issues.
    /// </summary>
    public static string GetUserFriendlyErrorMessage(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => 
                "アクセス権限がありません。\n" +
                "管理者権限でアプリケーションを実行するか、\n" +
                "ファイルのアクセス許可を確認してください。",
            
            JsonException => 
                "設定ファイルの形式が正しくありません。\n" +
                "JSONファイルの構文を確認してください。\n" +
                "バックアップがある場合は復元を試みてください。",
            
            FileNotFoundException => 
                "設定ファイルが見つかりません。\n" +
                "デフォルト設定で新規作成します。",
            
            DirectoryNotFoundException => 
                "設定ファイルのディレクトリが見つかりません。\n" +
                "ディレクトリを作成します。",
            
            IOException => 
                "設定ファイルの読み書きに失敗しました。\n" +
                "ファイルが他のプログラムで開かれていないか確認してください。",
            
            _ => 
                $"予期しないエラーが発生しました: {ex.Message}"
        };
    }
}
