using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceWatcher.Models;

namespace ServiceWatcher.Utils;

/// <summary>
/// 簡易設定読み込みクラス（US2簡易実装）
/// コンパイラバグ回避のため、複雑な検証やイベントなしで実装
/// Legacy implementation used by UI forms.
/// </summary>
[ExcludeFromCodeCoverage] // UI直接使用のレガシー実装、単体テスト対象外
public static class SimpleConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// 設定ファイルから監視対象サービスのリストを読み込む
    /// </summary>
    public static List<MonitoredService> LoadServices(string? configPath = null)
    {
        try
        {
            var path = configPath ?? GetDefaultConfigPath();
            
            if (!File.Exists(path))
            {
                return new List<MonitoredService>();
            }

            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<SimpleConfig>(json, JsonOptions);
            
            return config?.Services ?? new List<MonitoredService>();
        }
        catch
        {
            return new List<MonitoredService>();
        }
    }

    /// <summary>
    /// 監視間隔を読み込む（デフォルト: 5秒）
    /// </summary>
    public static int LoadMonitoringInterval(string? configPath = null)
    {
        try
        {
            var path = configPath ?? GetDefaultConfigPath();
            
            if (!File.Exists(path))
            {
                return 5;
            }

            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<SimpleConfig>(json, JsonOptions);
            
            return config?.MonitoringIntervalSeconds ?? 5;
        }
        catch
        {
            return 5;
        }
    }

    /// <summary>
    /// デフォルトの設定ファイルパスを取得
    /// プロジェクトルートのconfig.jsonを使用
    /// </summary>
    public static string GetDefaultConfigPath()
    {
        // 実行ファイルのディレクトリから config.json を探す
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(exeDir, "config.json");
        
        if (File.Exists(configPath))
        {
            return configPath;
        }

        // プロジェクトルートの config.json を探す（開発時用）
        var projectRoot = Directory.GetParent(exeDir)?.Parent?.Parent?.Parent?.FullName;
        if (projectRoot != null)
        {
            configPath = Path.Combine(projectRoot, "config.json");
            if (File.Exists(configPath))
            {
                return configPath;
            }
        }

        // デフォルトパス
        return Path.Combine(exeDir, "config.json");
    }

    /// <summary>
    /// 簡易設定クラス（JSON読み込み用）
    /// </summary>
    private class SimpleConfig
    {
        [JsonPropertyName("monitoringIntervalSeconds")]
        public int MonitoringIntervalSeconds { get; set; } = 5;

        [JsonPropertyName("monitoredServices")]
        public List<MonitoredService> Services { get; set; } = new();
    }
}
