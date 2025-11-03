using Microsoft.Extensions.Logging;
using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Service for displaying service status notifications to the user.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly Dictionary<string, Form> _activeNotifications;
    private readonly object _lock = new object();
    private readonly bool _simulate;

    /// <summary>
    /// Event raised when user acknowledges (closes) a notification.
    /// </summary>
    public event EventHandler<NotificationAcknowledgedEventArgs>? NotificationAcknowledged;

    /// <summary>
    /// Gets the count of currently displayed notifications.
    /// </summary>
    public int ActiveNotificationCount
    {
        get
        {
            lock (_lock)
            {
                return _activeNotifications.Count;
            }
        }
    }

    public NotificationService(ILogger<NotificationService> logger, bool simulate = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _synchronizationContext = SynchronizationContext.Current;
        _activeNotifications = new Dictionary<string, Form>(StringComparer.OrdinalIgnoreCase);
        _simulate = simulate;
    }

    /// <summary>
    /// Shows a notification for a service status change.
    /// </summary>
    public Result<bool> ShowNotification(ServiceStatusChange statusChange, int displayTimeSeconds)
    {
        try
        {
            if (statusChange == null)
            {
                return Result<bool>.Failure("Status change cannot be null");
            }

            if (displayTimeSeconds < 0 || displayTimeSeconds > 300)
            {
                return Result<bool>.Failure("Display time must be between 0 and 300 seconds");
            }

            // Check if notification already exists for this service
            lock (_lock)
            {
                if (_activeNotifications.ContainsKey(statusChange.ServiceName))
                {
                    _logger.LogDebug($"Notification for service '{statusChange.ServiceName}' already displayed");
                    return Result<bool>.Success(true);
                }
            }

            if (_simulate)
            {
                // テスト用: フォーム生成をスキップし辞書にプレースホルダ登録
                lock (_lock)
                {
                    _activeNotifications[statusChange.ServiceName] = new Form();
                }
            }
            else
            {
                // Create notification form on UI thread
                if (_synchronizationContext != null)
                {
                    _synchronizationContext.Post(_ => CreateNotificationForm(statusChange, displayTimeSeconds), null);
                }
                else
                {
                    // Fallback if no synchronization context (shouldn't happen in Windows Forms tests maybe)
                    CreateNotificationForm(statusChange, displayTimeSeconds);
                }
            }

            statusChange.NotificationShown = true;
            _logger.LogInformation($"Showed notification for service '{statusChange.ServiceName}': {statusChange.PreviousStatus} → {statusChange.CurrentStatus}");
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to show notification for service '{statusChange?.ServiceName}'");
            return Result<bool>.Failure(ex);
        }
    }

    /// <summary>
    /// Shows a notification asynchronously.
    /// </summary>
    public async Task<Result<bool>> ShowNotificationAsync(ServiceStatusChange statusChange, int displayTimeSeconds)
    {
        return await Task.Run(() => ShowNotification(statusChange, displayTimeSeconds));
    }

    /// <summary>
    /// Creates and displays a notification form (UI モードのみ)。
    /// </summary>
    private void CreateNotificationForm(ServiceStatusChange statusChange, int displayTimeSeconds)
    {
        var notificationForm = new Form
        {
            Text = $"サービス停止通知: {statusChange.DisplayName}",
            Size = new System.Drawing.Size(400, 150),
            StartPosition = FormStartPosition.Manual,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true
        };

        var label = new Label
        {
            Text = $"サービス '{statusChange.DisplayName}' が停止しました。\n\n" +
                   $"前の状態: {statusChange.PreviousStatus}\n" +
                   $"現在の状態: {statusChange.CurrentStatus}\n" +
                   $"検出時刻: {statusChange.DetectedAt:yyyy-MM-dd HH:mm:ss}",
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Padding = new Padding(10)
        };

        var closeButton = new Button
        {
            Text = "OK",
            Dock = DockStyle.Bottom,
            Height = 30
        };

        closeButton.Click += (s, e) =>
        {
            OnNotificationClosed(statusChange.ServiceName, false);
            notificationForm.Close();
        };

        notificationForm.Controls.Add(label);
        notificationForm.Controls.Add(closeButton);

        // Position at bottom-right of screen
        PositionNotificationForm(notificationForm);

        lock (_lock)
        {
            _activeNotifications[statusChange.ServiceName] = notificationForm;
        }

        notificationForm.FormClosed += (s, e) =>
        {
            lock (_lock)
            {
                _activeNotifications.Remove(statusChange.ServiceName);
            }
        };

        notificationForm.Show();

        // Auto-close timer if display time is specified
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
                OnNotificationClosed(statusChange.ServiceName, true);
                notificationForm.Close();
            };
            autoCloseTimer.Start();
        }
    }

    /// <summary>
    /// Positions notification form at bottom-right of screen with stacking.
    /// </summary>
    private void PositionNotificationForm(Form form)
    {
        var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        var workingArea = screen.WorkingArea;

        lock (_lock)
        {
            // Stack notifications vertically with 10px gap
            var offset = _activeNotifications.Count * (form.Height + 10);
            form.Location = new System.Drawing.Point(
                workingArea.Right - form.Width - 10,
                workingArea.Bottom - form.Height - 10 - offset
            );
        }
    }

    /// <summary>
    /// Closes all active notifications.
    /// </summary>
    public void CloseAllNotifications()
    {
        List<Form> formsToClose;
        lock (_lock)
        {
            formsToClose = new List<Form>(_activeNotifications.Values);
            _activeNotifications.Clear();
        }

        foreach (var form in formsToClose)
        {
            try
            {
                if (!_simulate)
                {
                    if (_synchronizationContext != null)
                    {
                        _synchronizationContext.Post(_ => form.Close(), null);
                    }
                    else
                    {
                        form.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to close notification form");
            }
        }

        _logger.LogInformation($"Closed {formsToClose.Count} notification(s)");
    }

    /// <summary>
    /// Closes a specific notification by service name.
    /// </summary>
    public bool CloseNotification(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return false;
        }

        Form? formToClose = null;
        lock (_lock)
        {
            if (_activeNotifications.TryGetValue(serviceName, out formToClose))
            {
                _activeNotifications.Remove(serviceName);
            }
        }

        if (formToClose != null)
        {
            try
            {
                if (!_simulate)
                {
                    if (_synchronizationContext != null)
                    {
                        _synchronizationContext.Post(_ => formToClose.Close(), null);
                    }
                    else
                    {
                        formToClose.Close();
                    }
                }

                OnNotificationClosed(serviceName, false);
                _logger.LogInformation($"Closed notification for service '{serviceName}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to close notification for service '{serviceName}'");
            }
        }

        return false;
    }

    /// <summary>
    /// Gets list of active notifications.
    /// </summary>
    public IReadOnlyList<ActiveNotificationInfo> GetActiveNotifications()
    {
        lock (_lock)
        {
            return _activeNotifications.Select(kvp => new ActiveNotificationInfo
            {
                ServiceName = kvp.Key,
                ShownAt = DateTime.Now, // Would need to track actual show time
                IsAutoClose = true // Would need to track from creation
            }).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Raises the NotificationAcknowledged event.
    /// </summary>
    private void OnNotificationClosed(string serviceName, bool wasAutoClosed)
    {
        var args = new NotificationAcknowledgedEventArgs(serviceName, wasAutoClosed);
        NotificationAcknowledged?.Invoke(this, args);
        _logger.LogDebug($"Notification acknowledged for service '{serviceName}' (auto-closed: {wasAutoClosed})");
    }
}
