using System.Globalization;

namespace ServiceWatcher.Models;

/// <summary>
/// Represents the application configuration loaded from config.json.
/// </summary>
public class ApplicationConfiguration
{
    /// <summary>
    /// Detects the default language based on OS culture.
    /// </summary>
    private static string DetectDefaultLanguage()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return culture.ToLowerInvariant() == "ja" ? "ja" : "en";
    }

    /// <summary>
    /// Gets or sets the monitoring interval in seconds.
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the notification display time in seconds (0 = manual close).
    /// </summary>
    public int NotificationDisplayTimeSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the UI language ("ja" for Japanese, "en" for English).
    /// </summary>
    public string UiLanguage { get; set; } = DetectDefaultLanguage();

    /// <summary>
    /// Gets or sets whether to start minimized to system tray.
    /// </summary>
    public bool StartMinimized { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to automatically start monitoring on application startup.
    /// </summary>
    public bool AutoStartMonitoring { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of services being monitored.
    /// </summary>
    public List<MonitoredService> MonitoredServices { get; set; } = new();
}
