namespace ServiceWatcher.Models;

/// <summary>
/// Represents a service status change event.
/// </summary>
public class ServiceStatusChange
{
    /// <summary>
    /// Gets or sets the internal service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable service name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous service status.
    /// </summary>
    public ServiceStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the current service status.
    /// </summary>
    public ServiceStatus CurrentStatus { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the change was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Gets or sets whether a notification was shown for this change.
    /// </summary>
    public bool NotificationShown { get; set; }

    /// <summary>
    /// Gets or sets whether the user acknowledged the notification.
    /// </summary>
    public bool UserAcknowledged { get; set; }

    /// <summary>
    /// Gets whether this change represents a service stop event.
    /// </summary>
    public bool IsStopEvent =>
        (PreviousStatus == ServiceStatus.Running || 
         PreviousStatus == ServiceStatus.Paused ||
         PreviousStatus == ServiceStatus.StartPending ||
         PreviousStatus == ServiceStatus.ContinuePending) &&
        (CurrentStatus == ServiceStatus.Stopped ||
         CurrentStatus == ServiceStatus.StopPending);
}
