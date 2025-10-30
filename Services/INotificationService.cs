using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Interface for displaying service status notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets the count of currently displayed notifications.
    /// </summary>
    int ActiveNotificationCount { get; }
    
    /// <summary>
    /// Event raised when user acknowledges (closes) a notification.
    /// </summary>
    event EventHandler<NotificationAcknowledgedEventArgs> NotificationAcknowledged;
    
    /// <summary>
    /// Shows a notification for a service status change.
    /// </summary>
    /// <param name="statusChange">The status change to notify about.</param>
    /// <param name="displayTimeSeconds">How long to display (0 = infinite).</param>
    /// <returns>Result indicating success or failure.</returns>
    Result<bool> ShowNotification(ServiceStatusChange statusChange, int displayTimeSeconds);
    
    /// <summary>
    /// Shows a notification asynchronously.
    /// </summary>
    /// <param name="statusChange">The status change to notify about.</param>
    /// <param name="displayTimeSeconds">How long to display (0 = infinite).</param>
    /// <returns>Task result indicating success or failure.</returns>
    Task<Result<bool>> ShowNotificationAsync(ServiceStatusChange statusChange, int displayTimeSeconds);
    
    /// <summary>
    /// Closes all active notifications.
    /// </summary>
    void CloseAllNotifications();
    
    /// <summary>
    /// Closes a specific notification by service name.
    /// </summary>
    /// <param name="serviceName">Service name whose notification to close.</param>
    /// <returns>True if notification was found and closed.</returns>
    bool CloseNotification(string serviceName);
    
    /// <summary>
    /// Gets list of active notifications.
    /// </summary>
    /// <returns>Read-only list of active notification info.</returns>
    IReadOnlyList<ActiveNotificationInfo> GetActiveNotifications();
}

/// <summary>
/// Information about an active notification.
/// </summary>
public class ActiveNotificationInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ShownAt { get; set; }
    public int DisplayTimeSeconds { get; set; }
    public bool IsAutoClose { get; set; }
}

/// <summary>
/// Event args for notification acknowledgement.
/// </summary>
public class NotificationAcknowledgedEventArgs : EventArgs
{
    public string ServiceName { get; }
    public DateTime AcknowledgedAt { get; }
    public bool WasAutoClosed { get; }
    
    public NotificationAcknowledgedEventArgs(string serviceName, bool wasAutoClosed)
    {
        ServiceName = serviceName;
        AcknowledgedAt = DateTime.Now;
        WasAutoClosed = wasAutoClosed;
    }
}
