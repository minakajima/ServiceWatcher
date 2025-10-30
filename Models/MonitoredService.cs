using System.Text.Json.Serialization;

namespace ServiceWatcher.Models;

/// <summary>
/// Represents a Windows service that is being monitored for status changes.
/// </summary>
public class MonitoredService
{
    /// <summary>
    /// Gets or sets the internal Windows service name (e.g., "wuauserv").
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable service name (e.g., "Windows Update").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to show notifications for this service.
    /// </summary>
    public bool NotificationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the last recorded service status.
    /// </summary>
    public ServiceStatus LastKnownStatus { get; set; } = ServiceStatus.Unknown;

    /// <summary>
    /// Gets or sets the timestamp of the last status check.
    /// </summary>
    public DateTime? LastChecked { get; set; }

    /// <summary>
    /// Gets or sets whether the service is accessible (exists and permissions allow).
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if the service is unavailable.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Validates the MonitoredService instance.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(ServiceName))
            return false;

        if (ServiceName.Length > 256)
            return false;

        if (string.IsNullOrWhiteSpace(DisplayName))
            return false;

        if (DisplayName.Length > 256)
            return false;

        // LastChecked must be in the past or null
        if (LastChecked.HasValue && LastChecked.Value > DateTime.Now)
            return false;

        // If unavailable, there should be an error message
        if (!IsAvailable && string.IsNullOrWhiteSpace(ErrorMessage))
            return false;

        return true;
    }
}
