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
        RefreshServiceList();
        
        // Setup keyboard shortcuts
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;
        
        // Load window state
        LoadWindowState();
    }

    /// <summary>
    /// Handles keyboard shortcuts.
    /// </summary>
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // F5: Refresh service list
        if (e.KeyCode == Keys.F5)
        {
            RefreshServiceList();
            e.Handled = true;
        }
        // Escape: Stop monitoring if running
        else if (e.KeyCode == Keys.Escape && _serviceMonitor.IsMonitoring)
        {
            btnStop_Click(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the Start Monitoring button click.
    /// </summary>
    private async void btnStart_Click(object sender, EventArgs e)
    {
        try
        {
            // Validate configuration file
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            
            if (!File.Exists(configPath))
            {
                var createDialog = MessageBox.Show(
                    "config.jsonファイルが見つかりません。\n\n" +
                    "デフォルトの設定ファイルを作成しますか？",
                    "設定ファイルなし",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (createDialog == DialogResult.Yes)
                {
                    var createResult = ConfigurationHelper.CreateDefaultConfig(configPath, _logger);
                    if (!createResult.IsSuccess)
                    {
                        MessageBox.Show(createResult.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            var validateResult = ConfigurationHelper.ValidateAndRepairConfig(configPath, _logger);
            if (!validateResult.IsSuccess)
            {
                var restoreDialog = MessageBox.Show(
                    validateResult.Error + "\n\nバックアップから復元しますか？",
                    "設定ファイルエラー",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (restoreDialog == DialogResult.Yes)
                {
                    var restoreResult = ConfigurationHelper.RestoreFromBackup(configPath, _logger);
                    if (!restoreResult.IsSuccess)
                    {
                        MessageBox.Show(restoreResult.Error, "復元失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // Load services from config.json (US2 simple implementation)
            var services = SimpleConfigLoader.LoadServices();
            var interval = SimpleConfigLoader.LoadMonitoringInterval();
            
            if (services.Count == 0)
            {
                MessageBox.Show(
                    "config.jsonに監視対象サービスが設定されていません。\n\n" +
                    "「サービス管理」ボタンから監視対象サービスを追加してください。",
                    "設定エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            
            // Update monitoring interval before adding services
            var intervalResult = await _serviceMonitor.UpdateMonitoringIntervalAsync(interval);
            if (!intervalResult.IsSuccess)
            {
                _logger.LogWarning($"Failed to update monitoring interval: {intervalResult.Error}");
            }
            
            // Add services from configuration
            foreach (var service in services)
            {
                await _serviceMonitor.AddServiceAsync(service);
                _logger.LogInformation($"Added service from config: {service.ServiceName}");
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            var result = await _serviceMonitor.StartMonitoringAsync(_cancellationTokenSource.Token);
            
            if (result.IsSuccess)
            {
                UpdateStatusLabel($"監視中 - {_serviceMonitor.MonitoredServices.Count}個のサービス");
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                _logger.LogInformation($"Started monitoring {services.Count} services with {interval}s interval");
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
    /// Opens the service management form.
    /// </summary>
    private void btnManageServices_Click(object sender, EventArgs e)
    {
        try
        {
            var loggerFactory = new Utils.LoggerFactory();
            var serviceListForm = new ServiceListForm(
                _serviceMonitor,
                loggerFactory.CreateLogger<ServiceListForm>());
            
            if (serviceListForm.ShowDialog() == DialogResult.OK)
            {
                RefreshServiceList();
                _logger.LogInformation("Service list updated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening service list form");
            MessageBox.Show($"サービス管理画面の表示に失敗しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Opens the settings form.
    /// </summary>
    private void btnSettings_Click(object sender, EventArgs e)
    {
        try
        {
            var loggerFactory = new Utils.LoggerFactory();
            var settingsForm = new SettingsForm(loggerFactory.CreateLogger<SettingsForm>());
            
            settingsForm.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings form");
            MessageBox.Show($"設定画面の表示に失敗しました: {ex.Message}", "エラー",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Refreshes the monitored services display.
    /// </summary>
    private void RefreshServiceList()
    {
        try
        {
            var services = SimpleConfigLoader.LoadServices();
            dgvMonitoredServices.DataSource = null;
            dgvMonitoredServices.DataSource = services.Select(s => new
            {
                ServiceName = s.ServiceName,
                DisplayName = s.DisplayName,
                NotificationEnabled = s.NotificationEnabled ? "有効" : "無効",
                Status = s.LastKnownStatus.ToString()
            }).ToList();

            if (dgvMonitoredServices.Columns.Count > 0)
            {
                dgvMonitoredServices.Columns["ServiceName"].HeaderText = "サービス名";
                dgvMonitoredServices.Columns["DisplayName"].HeaderText = "表示名";
                dgvMonitoredServices.Columns["NotificationEnabled"].HeaderText = "通知";
                dgvMonitoredServices.Columns["Status"].HeaderText = "状態";
            }

            _logger.LogInformation($"Refreshed service list: {services.Count} services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing service list");
        }
    }

    /// <summary>
    /// Loads window state from config.
    /// </summary>
    private void LoadWindowState()
    {
        try
        {
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            if (!File.Exists(configPath))
                return;

            var json = File.ReadAllText(configPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("windowState", out var windowState))
            {
                if (windowState.TryGetProperty("x", out var x) &&
                    windowState.TryGetProperty("y", out var y) &&
                    windowState.TryGetProperty("width", out var width) &&
                    windowState.TryGetProperty("height", out var height))
                {
                    var bounds = new Rectangle(x.GetInt32(), y.GetInt32(), width.GetInt32(), height.GetInt32());
                    
                    // Ensure window is visible on current screen(s)
                    if (IsVisibleOnAnyScreen(bounds))
                    {
                        this.StartPosition = FormStartPosition.Manual;
                        this.Location = bounds.Location;
                        this.Size = bounds.Size;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load window state");
        }
    }

    /// <summary>
    /// Checks if rectangle is visible on any screen.
    /// </summary>
    private bool IsVisibleOnAnyScreen(Rectangle bounds)
    {
        foreach (var screen in Screen.AllScreens)
        {
            if (screen.WorkingArea.IntersectsWith(bounds))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Saves window state to config.
    /// </summary>
    private void SaveWindowState()
    {
        try
        {
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            if (!File.Exists(configPath))
                return;

            var json = File.ReadAllText(configPath);
            var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
            
            if (config == null)
                return;

            // Save current window position and size
            var windowState = new
            {
                x = this.Location.X,
                y = this.Location.Y,
                width = this.Size.Width,
                height = this.Size.Height
            };

            config["windowState"] = System.Text.Json.JsonSerializer.SerializeToElement(windowState);
            config["lastModified"] = System.Text.Json.JsonSerializer.SerializeToElement(DateTime.Now);

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var updatedJson = System.Text.Json.JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, updatedJson);

            _logger.LogInformation("Saved window state");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save window state");
        }
    }

    /// <summary>
    /// Handles form closing event.
    /// </summary>
    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        SaveWindowState();
        
        if (_serviceMonitor.IsMonitoring)
        {
            await _serviceMonitor.StopMonitoringAsync();
        }
        
        _serviceMonitor.Dispose();
        _notificationService.CloseAllNotifications();
        
        base.OnFormClosing(e);
    }
}
