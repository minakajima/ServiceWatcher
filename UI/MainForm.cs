using Microsoft.Extensions.Logging;
using ServiceWatcher.Services;
using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.UI;

/// <summary>
/// Main application window for service monitoring.
/// </summary>
public partial class MainForm : Form
{
    private readonly IServiceMonitor _serviceMonitor;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MainForm> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainForm()
    {
        InitializeComponent();
        
        // Create logger
        var loggerFactory = new Utils.LoggerFactory();
        _logger = loggerFactory.CreateLogger<MainForm>();
        
        // Create services
        _serviceMonitor = new ServiceMonitor(loggerFactory.CreateLogger<ServiceMonitor>());
        _notificationService = new NotificationService(loggerFactory.CreateLogger<NotificationService>());
        
        // Wire events
        _serviceMonitor.ServiceStatusChanged += OnServiceStatusChanged;
        _serviceMonitor.MonitoringError += OnMonitoringError;
        
        // Initialize UI
        UpdateStatusLabel("準備完了");
        btnStop.Enabled = false;
    }

    /// <summary>
    /// Handles the Start Monitoring button click.
    /// </summary>
    private async void btnStart_Click(object sender, EventArgs e)
    {
        try
        {
            // Add a test service for MVP (will be configurable in US2)
            var testService = new MonitoredService
            {
                ServiceName = "wuauserv",
                DisplayName = "Windows Update",
                NotificationEnabled = true
            };
            
            await _serviceMonitor.AddServiceAsync(testService);
            
            _cancellationTokenSource = new CancellationTokenSource();
            var result = await _serviceMonitor.StartMonitoringAsync(_cancellationTokenSource.Token);
            
            if (result.IsSuccess)
            {
                UpdateStatusLabel($"監視中 - {_serviceMonitor.MonitoredServices.Count}個のサービス");
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                _logger.LogInformation("Started monitoring services");
            }
            else
            {
                MessageBox.Show($"監視の開始に失敗しました: {result.Error}", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting monitoring");
            MessageBox.Show($"エラーが発生しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Handles the Stop Monitoring button click.
    /// </summary>
    private async void btnStop_Click(object sender, EventArgs e)
    {
        try
        {
            var result = await _serviceMonitor.StopMonitoringAsync();
            
            if (result.IsSuccess)
            {
                UpdateStatusLabel("監視停止");
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _logger.LogInformation("Stopped monitoring services");
            }
            else
            {
                MessageBox.Show($"監視の停止に失敗しました: {result.Error}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping monitoring");
            MessageBox.Show($"エラーが発生しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Handles service status change events.
    /// </summary>
    private void OnServiceStatusChanged(object? sender, ServiceStatusChangeEventArgs e)
    {
        _logger.LogInformation($"Service status changed: {e.StatusChange.ServiceName} - {e.StatusChange.PreviousStatus} → {e.StatusChange.CurrentStatus}");
        
        // Show notification if it's a stop event
        if (e.StatusChange.IsStopEvent)
        {
            _notificationService.ShowNotification(e.StatusChange, 30);
        }
    }

    /// <summary>
    /// Handles monitoring error events.
    /// </summary>
    private void OnMonitoringError(object? sender, MonitoringErrorEventArgs e)
    {
        _logger.LogWarning($"Monitoring error for service '{e.ServiceName}': {e.ErrorMessage}");
        
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatusLabel($"エラー: {e.ServiceName}"));
        }
        else
        {
            UpdateStatusLabel($"エラー: {e.ServiceName}");
        }
    }

    /// <summary>
    /// Updates the status label text.
    /// </summary>
    private void UpdateStatusLabel(string text)
    {
        if (InvokeRequired)
        {
            Invoke(() => lblStatus.Text = text);
        }
        else
        {
            lblStatus.Text = text;
        }
    }

    /// <summary>
    /// Handles form closing event.
    /// </summary>
    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (_serviceMonitor.IsMonitoring)
        {
            await _serviceMonitor.StopMonitoringAsync();
        }
        
        _serviceMonitor.Dispose();
        _notificationService.CloseAllNotifications();
        
        base.OnFormClosing(e);
    }
}
