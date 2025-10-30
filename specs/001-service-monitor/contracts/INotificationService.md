# INotificationService Contract

**Version**: 1.0  
**Status**: Draft  
**Last Updated**: 2025-10-30

## Purpose

Defines the interface for displaying user notifications when monitored services stop. Handles popup window creation, display duration, and user interaction tracking.

## Interface Definition

```csharp
namespace ServiceWatcher.Services
{
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
}
```

## Usage Example

### Showing Notification from ServiceMonitor

```csharp
public class ServiceMonitor : IServiceMonitor
{
    private readonly INotificationService _notificationService;
    private readonly IConfigurationManager _configManager;
    
    public ServiceMonitor(
        ILogger<ServiceMonitor> logger,
        IConfigurationManager configManager,
        INotificationService notificationService)
    {
        _logger = logger;
        _configManager = configManager;
        _notificationService = notificationService;
    }
    
    private async Task CheckAllServicesAsync()
    {
        foreach (var service in _monitoredServices)
        {
            var previousStatus = service.LastKnownStatus;
            var currentStatus = await GetCurrentStatusAsync(service.ServiceName);
            
            // Detect status change
            if (previousStatus == ServiceStatus.Running && 
                currentStatus == ServiceStatus.Stopped &&
                service.NotificationEnabled)
            {
                var statusChange = new ServiceStatusChange
                {
                    ServiceName = service.ServiceName,
                    DisplayName = service.DisplayName,
                    PreviousStatus = previousStatus,
                    CurrentStatus = currentStatus,
                    DetectedAt = DateTime.Now
                };
                
                // Show notification
                var displayTime = _configManager.Configuration.NotificationDisplayTimeSeconds;
                var result = _notificationService.ShowNotification(statusChange, displayTime);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Notification shown for service {ServiceName}", 
                        service.ServiceName);
                }
                else
                {
                    _logger.LogWarning("Failed to show notification: {Error}", 
                        result.ErrorMessage);
                }
                
                // Raise event
                ServiceStatusChanged?.Invoke(this, 
                    new ServiceStatusChangeEventArgs(statusChange));
            }
        }
    }
}
```

### Handling Notification Acknowledgement

```csharp
public class MainForm : Form
{
    private readonly INotificationService _notificationService;
    
    public MainForm(INotificationService notificationService)
    {
        _notificationService = notificationService;
        
        // Subscribe to notification events
        _notificationService.NotificationAcknowledged += OnNotificationAcknowledged;
    }
    
    private void OnNotificationAcknowledged(object? sender, NotificationAcknowledgedEventArgs e)
    {
        _logger.LogInformation("User acknowledged notification for {ServiceName} (AutoClosed: {AutoClosed})",
            e.ServiceName, e.WasAutoClosed);
        
        // Update notification history in UI
        AddToNotificationHistory(e.ServiceName, e.AcknowledgedAt, e.WasAutoClosed);
    }
    
    private void CloseAllNotificationsButton_Click(object sender, EventArgs e)
    {
        _notificationService.CloseAllNotifications();
        StatusLabel.Text = "すべての通知を閉じました";
    }
}
```

## Implementation Requirements

### Constructor

```csharp
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly Dictionary<string, NotificationForm> _activeNotifications;
    private readonly SynchronizationContext _syncContext;
    
    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeNotifications = new Dictionary<string, NotificationForm>();
        
        // Capture UI synchronization context
        _syncContext = SynchronizationContext.Current ?? 
            throw new InvalidOperationException("Must be created on UI thread");
    }
    
    public int ActiveNotificationCount => _activeNotifications.Count;
}
```

### ShowNotification Implementation

```csharp
public Result<bool> ShowNotification(ServiceStatusChange statusChange, int displayTimeSeconds)
{
    try
    {
        _logger.LogInformation("Showing notification for service {ServiceName}", 
            statusChange.ServiceName);
        
        // Check if notification already exists for this service
        if (_activeNotifications.ContainsKey(statusChange.ServiceName))
        {
            _logger.LogDebug("Notification already exists for {ServiceName}, skipping", 
                statusChange.ServiceName);
            return Result<bool>.Success(true);
        }
        
        // Create notification form on UI thread
        _syncContext.Post(_ =>
        {
            try
            {
                var notificationForm = new NotificationForm(statusChange, displayTimeSeconds);
                
                // Position form at bottom-right of screen
                var screen = Screen.PrimaryScreen;
                var workingArea = screen.WorkingArea;
                var stackOffset = _activeNotifications.Count * (notificationForm.Height + 10);
                
                notificationForm.StartPosition = FormStartPosition.Manual;
                notificationForm.Location = new Point(
                    workingArea.Right - notificationForm.Width - 10,
                    workingArea.Bottom - notificationForm.Height - stackOffset - 10
                );
                
                // Setup event handlers
                notificationForm.FormClosed += (s, e) => OnNotificationClosed(statusChange.ServiceName, false);
                
                // Setup auto-close timer if needed
                if (displayTimeSeconds > 0)
                {
                    var autoCloseTimer = new System.Windows.Forms.Timer
                    {
                        Interval = displayTimeSeconds * 1000
                    };
                    autoCloseTimer.Tick += (s, e) =>
                    {
                        autoCloseTimer.Stop();
                        autoCloseTimer.Dispose();
                        notificationForm.Close();
                        OnNotificationClosed(statusChange.ServiceName, true);
                    };
                    autoCloseTimer.Start();
                }
                
                // Track and show
                _activeNotifications[statusChange.ServiceName] = notificationForm;
                notificationForm.Show();
                
                _logger.LogInformation("Notification displayed for {ServiceName}", 
                    statusChange.ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification form");
            }
        }, null);
        
        return Result<bool>.Success(true);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to show notification");
        return Result<bool>.Failure($"通知表示エラー: {ex.Message}");
    }
}

private void OnNotificationClosed(string serviceName, bool wasAutoClosed)
{
    lock (_activeNotifications)
    {
        _activeNotifications.Remove(serviceName);
    }
    
    _logger.LogDebug("Notification closed for {ServiceName} (AutoClosed: {AutoClosed})",
        serviceName, wasAutoClosed);
    
    // Raise event
    NotificationAcknowledged?.Invoke(this, 
        new NotificationAcknowledgedEventArgs(serviceName, wasAutoClosed));
}
```

### CloseAllNotifications Implementation

```csharp
public void CloseAllNotifications()
{
    _logger.LogInformation("Closing all {Count} notifications", _activeNotifications.Count);
    
    _syncContext.Post(_ =>
    {
        // Create copy to avoid collection modification during enumeration
        var notificationsToClose = _activeNotifications.Values.ToList();
        
        foreach (var notification in notificationsToClose)
        {
            try
            {
                notification.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing notification");
            }
        }
        
        _activeNotifications.Clear();
    }, null);
}
```

## NotificationForm Design

### Form Properties

```csharp
public class NotificationForm : Form
{
    public NotificationForm(ServiceStatusChange statusChange, int displayTimeSeconds)
    {
        // Form settings
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(300, 120);
        
        // Build UI
        BuildUI(statusChange, displayTimeSeconds);
    }
    
    private void BuildUI(ServiceStatusChange statusChange, int displayTimeSeconds)
    {
        BackColor = Color.FromArgb(255, 240, 240); // Light red background
        
        // Icon
        var iconLabel = new Label
        {
            Text = "⚠",
            Font = new Font("Segoe UI", 32, FontStyle.Bold),
            ForeColor = Color.Red,
            AutoSize = true,
            Location = new Point(10, 10)
        };
        
        // Service name
        var serviceLabel = new Label
        {
            Text = statusChange.DisplayName,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(60, 15)
        };
        
        // Message
        var messageLabel = new Label
        {
            Text = "サービスが停止しました",
            Font = new Font("Segoe UI", 9),
            AutoSize = true,
            Location = new Point(60, 40)
        };
        
        // Timestamp
        var timeLabel = new Label
        {
            Text = statusChange.DetectedAt.ToString("HH:mm:ss"),
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(60, 60)
        };
        
        // Close button
        var closeButton = new Button
        {
            Text = "OK",
            Size = new Size(75, 25),
            Location = new Point(200, 80)
        };
        closeButton.Click += (s, e) => Close();
        
        Controls.AddRange(new Control[] 
        { 
            iconLabel, serviceLabel, messageLabel, timeLabel, closeButton 
        });
    }
}
```

## Error Handling

### Expected Scenarios

| Scenario | Handling |
|----------|----------|
| UI thread not available | Throw exception in constructor |
| Duplicate notification | Skip, return success |
| Form creation fails | Log error, return failure |
| Auto-close timer fails | Log warning, notification stays open |

## Performance Requirements

| Operation | Max Duration | Notes |
|-----------|--------------|-------|
| ShowNotification | <50ms | Form creation and display |
| CloseAllNotifications | <100ms | Close all forms |
| GetActiveNotifications | <10ms | Read dictionary |

## Thread Safety

- All public methods are thread-safe
- UI operations marshaled to UI thread via `SynchronizationContext`
- Internal dictionary protected by lock

## UI Requirements

### Notification Positioning

```
┌─────────────────────────────────────┐
│         Screen Working Area         │
│                                     │
│                                     │
│                                     │
│                    ┌──────────────┐ │
│                    │ Notification │ │ ← 3rd (offset)
│                    └──────────────┘ │
│                    ┌──────────────┐ │
│                    │ Notification │ │ ← 2nd (offset)
│                    └──────────────┘ │
│                    ┌──────────────┐ │
│                    │ Notification │ │ ← 1st (bottom-right)
│                    └──────────────┘ │
└─────────────────────────────────────┘
```

- **Position**: Bottom-right corner
- **Stacking**: Vertical, upward
- **Spacing**: 10px between notifications
- **Z-Order**: TopMost = true

### Notification Content

**Required Elements**:
- Service display name
- Status message ("サービスが停止しました")
- Timestamp (HH:mm:ss)
- Close button
- Warning icon

**Optional Elements** (future):
- Service restart button
- "Don't show again" checkbox
- View logs button

## Testing Checklist

- [ ] Show single notification
- [ ] Show multiple notifications (stacking)
- [ ] Auto-close after timeout
- [ ] Manual close with button
- [ ] Close all notifications
- [ ] Duplicate notification handling
- [ ] Notification positioning (screen edges)
- [ ] Multi-monitor support
- [ ] Notification event raised
- [ ] Thread safety (show from background thread)
- [ ] Memory leaks (show/close many notifications)
- [ ] UI responsiveness

## Dependencies

- `Microsoft.Extensions.Logging.ILogger<NotificationService>`
- `System.Windows.Forms` (Form, Timer, Screen)
- `System.Threading.SynchronizationContext`

## Future Enhancements

### Version 2.0 Candidates

1. **Notification History**: Persist closed notifications to view later
2. **Sound Alerts**: Optional sound when notification appears
3. **Notification Templates**: Customizable notification appearance
4. **Action Buttons**: Restart service directly from notification
5. **System Tray Integration**: Show notification count in tray icon
6. **Rich Notifications**: Windows 10+ toast notifications (WinRT API)
