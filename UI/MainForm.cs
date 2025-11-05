using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Services;
using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.UI;

/// <summary>
/// Main application window for service monitoring.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class MainForm : Form
{
    private readonly IServiceMonitor _serviceMonitor;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MainForm> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainForm()
    {
        InitializeComponent();
        
        // Create logger
        var loggerFactory = new Utils.LoggerFactory();
        _logger = loggerFactory.CreateLogger<MainForm>();
        
        // Create services
        _localizationService = new LocalizationService(loggerFactory.CreateLogger<LocalizationService>());
        _serviceMonitor = new ServiceMonitor(loggerFactory.CreateLogger<ServiceMonitor>());
        _notificationService = new NotificationService(loggerFactory.CreateLogger<NotificationService>(), _localizationService);
        
        // Apply localization to the form
        ApplyLocalization();
        
        // Wire events
        _serviceMonitor.ServiceStatusChanged += OnServiceStatusChanged;
        _serviceMonitor.MonitoringError += OnMonitoringError;
        
        // Initialize UI
        UpdateStatusLabel(_localizationService.GetString("MainForm_Ready") ?? "準備完了");
        btnStop.Enabled = false;
        RefreshServiceList();
        
        // Setup keyboard shortcuts
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;
        
        // Load window state
        LoadWindowState();
        
        // Apply startup settings
        this.Load += MainForm_Load;
    }

    /// <summary>
    /// Applies localized text to all UI elements.
    /// </summary>
    private void ApplyLocalization()
    {
        // Form title
        this.Text = _localizationService.GetString("MainForm_Title") ?? "サービスウォッチャー";
        
        // Buttons
        btnStart.Text = _localizationService.GetString("MainForm_StartButton") ?? "監視開始";
        btnStop.Text = _localizationService.GetString("MainForm_StopButton") ?? "監視停止";
        btnManageServices.Text = _localizationService.GetString("MainForm_ManageServicesButton") ?? "サービス管理";
        btnSettings.Text = _localizationService.GetString("MainForm_SettingsButton") ?? "設定";
        
        // Labels
        lblTitle.Text = _localizationService.GetString("MainForm_Title") ?? "サービスウォッチャー";
        lblMonitoredServices.Text = _localizationService.GetString("MainForm_MonitoredServicesLabel") ?? "監視中のサービス:";
    }

    /// <summary>
    /// Handles form load event to apply startup settings.
    /// </summary>
    private async void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            if (!File.Exists(configPath))
                return;

            var json = File.ReadAllText(configPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Apply startMinimized setting
            if (root.TryGetProperty("startMinimized", out var startMinProp) && 
                startMinProp.GetBoolean())
            {
                this.WindowState = FormWindowState.Minimized;
                _logger.LogInformation("Application started minimized per configuration");
            }

            // Apply autoStartMonitoring setting
            if (root.TryGetProperty("autoStartMonitoring", out var autoStartProp) && 
                autoStartProp.GetBoolean())
            {
                _logger.LogInformation("Auto-starting monitoring per configuration");
                // Wait briefly for UI initialization to complete
                await Task.Delay(500);
                btnStart_Click(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply startup settings from configuration");
        }
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
                    _localizationService.GetString("MainForm_ConfigNotFoundMessage") ?? 
                    "config.jsonファイルが見つかりません。\n\nデフォルトの設定ファイルを作成しますか？",
                    _localizationService.GetString("MainForm_ConfigNotFoundTitle") ?? "設定ファイルなし",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (createDialog == DialogResult.Yes)
                {
                    var createResult = ConfigurationHelper.CreateDefaultConfig(configPath, _logger);
                    if (!createResult.IsSuccess)
                    {
                        MessageBox.Show(createResult.Error, 
                            _localizationService.GetString("Error_Title") ?? "エラー", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    validateResult.Error + "\n\n" + (_localizationService.GetString("MainForm_RestoreFromBackupPrompt") ?? "バックアップから復元しますか？"),
                    _localizationService.GetString("MainForm_ConfigErrorTitle") ?? "設定ファイルエラー",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (restoreDialog == DialogResult.Yes)
                {
                    var restoreResult = ConfigurationHelper.RestoreFromBackup(configPath, _logger);
                    if (!restoreResult.IsSuccess)
                    {
                        MessageBox.Show(restoreResult.Error, 
                            _localizationService.GetString("MainForm_RestoreFailedTitle") ?? "復元失敗", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    _localizationService.GetString("MainForm_NoServicesConfiguredMessage") ?? 
                    "config.jsonに監視対象サービスが設定されていません。\n\n「サービス管理」ボタンから監視対象サービスを追加してください。",
                    _localizationService.GetString("MainForm_ConfigErrorTitle") ?? "設定エラー",
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
                var statusText = _localizationService.GetFormattedString("MainForm_StatusMonitoring", _serviceMonitor.MonitoredServices.Count) 
                    ?? $"監視中 - {_serviceMonitor.MonitoredServices.Count}個のサービス";
                UpdateStatusLabel(statusText);
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                
                // Wait a moment for initial status check to complete, then refresh UI
                await Task.Delay(100);
                RefreshServiceList(); // Update list to show live monitoring data with initial status
                
                _logger.LogInformation($"Started monitoring {services.Count} services with {interval}s interval");
            }
            else
            {
                var errorMessage = _localizationService.GetString("Error_MonitoringStartFailed") ?? "監視の開始に失敗しました";
                var errorTitle = _localizationService.GetString("Error_Title") ?? "エラー";
                MessageBox.Show($"{errorMessage}: {result.Error}", errorTitle, 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting monitoring");
            var errorMessage = _localizationService.GetString("Error_ExceptionOccurred") ?? "エラーが発生しました";
            var errorTitle = _localizationService.GetString("Error_Title") ?? "エラー";
            MessageBox.Show($"{errorMessage}: {ex.Message}", errorTitle,
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
                var statusText = _localizationService.GetString("MainForm_StatusStopped") ?? "監視停止";
                UpdateStatusLabel(statusText);
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                RefreshServiceList(); // Update list to show config file data
                _logger.LogInformation("Stopped monitoring services");
            }
            else
            {
                var errorMessage = _localizationService.GetString("MainForm_StopMonitoringFailed") ?? "監視の停止に失敗しました";
                MessageBox.Show($"{errorMessage}: {result.Error}", 
                    _localizationService.GetString("Error_Title") ?? "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping monitoring");
            var errorMessage = _localizationService.GetString("Error_ExceptionOccurred") ?? "エラーが発生しました";
            MessageBox.Show($"{errorMessage}: {ex.Message}", 
                _localizationService.GetString("Error_Title") ?? "エラー",
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

        // Update UI to reflect status change
        if (InvokeRequired)
        {
            Invoke(() => RefreshServiceList());
        }
        else
        {
            RefreshServiceList();
        }
    }

    /// <summary>
    /// Handles monitoring error events.
    /// </summary>
    private void OnMonitoringError(object? sender, MonitoringErrorEventArgs e)
    {
        _logger.LogWarning($"Monitoring error for service '{e.ServiceName}': {e.ErrorMessage}");
        
        var errorText = _localizationService.GetFormattedString("MainForm_StatusError", e.ServiceName) 
            ?? $"エラー: {e.ServiceName}";
        
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatusLabel(errorText));
        }
        else
        {
            UpdateStatusLabel(errorText);
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
                loggerFactory.CreateLogger<ServiceListForm>(),
                _localizationService);
            
            if (serviceListForm.ShowDialog() == DialogResult.OK)
            {
                RefreshServiceList();
                _logger.LogInformation("Service list updated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening service list form");
            var errorMessage = _localizationService.GetString("Error_ServiceListFormFailed") ?? "サービス管理画面の表示に失敗しました";
            MessageBox.Show($"{errorMessage}: {ex.Message}", _localizationService.GetString("Error_Title") ?? "エラー",
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
            var settingsForm = new SettingsForm(
                loggerFactory.CreateLogger<SettingsForm>(), 
                _localizationService);
            
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                // Settings were saved, refresh UI
                RefreshServiceList();
                
                // Re-apply localization to this form
                ApplyLocalization();
                
                // Update status text with localized string
                if (_serviceMonitor.IsMonitoring)
                {
                    var statusText = _localizationService.GetFormattedString("MainForm_StatusMonitoring", _serviceMonitor.MonitoredServices.Count) 
                        ?? $"監視中 - {_serviceMonitor.MonitoredServices.Count}個のサービス";
                    UpdateStatusLabel(statusText);
                }
                else
                {
                    UpdateStatusLabel(_localizationService.GetString("MainForm_Ready") ?? "準備完了");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings form");
            var errorMessage = _localizationService.GetString("Error_SettingsFormFailed") ?? "設定画面の表示に失敗しました";
            MessageBox.Show($"{errorMessage}: {ex.Message}", _localizationService.GetString("Error_Title") ?? "エラー",
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
            // If monitoring is active, use live data from ServiceMonitor
            // Otherwise, load from config file and get current status
            List<MonitoredService> services;
            
            if (_serviceMonitor.IsMonitoring)
            {
                services = _serviceMonitor.MonitoredServices.ToList();
            }
            else
            {
                // Load from config and get current system status
                services = SimpleConfigLoader.LoadServices();
                foreach (var service in services)
                {
                    try
                    {
                        using var controller = new System.ServiceProcess.ServiceController(service.ServiceName);
                        controller.Refresh();
                        service.LastKnownStatus = controller.Status.ToServiceStatus();
                    }
                    catch
                    {
                        service.LastKnownStatus = ServiceStatus.Unknown;
                    }
                }
            }
            
            _logger.LogInformation($"RefreshServiceList: IsMonitoring={_serviceMonitor.IsMonitoring}, Count={services.Count}");
            foreach (var svc in services)
            {
                _logger.LogInformation($"  - {svc.ServiceName}: {svc.LastKnownStatus}");
            }
            
            var enabledText = _localizationService.GetString("MainForm_Enabled") ?? "有効";
            var disabledText = _localizationService.GetString("MainForm_Disabled") ?? "無効";
            
            dgvMonitoredServices.DataSource = null;
            dgvMonitoredServices.DataSource = services.Select(s => new
            {
                ServiceName = s.ServiceName,
                DisplayName = s.DisplayName,
                NotificationEnabled = s.NotificationEnabled ? enabledText : disabledText,
                Status = s.LastKnownStatus.ToString()
            }).ToList();

            if (dgvMonitoredServices.Columns.Count > 0)
            {
                dgvMonitoredServices.Columns["ServiceName"].HeaderText = _localizationService.GetString("MainForm_ColumnServiceName") ?? "サービス名";
                dgvMonitoredServices.Columns["DisplayName"].HeaderText = _localizationService.GetString("MainForm_ColumnDisplayName") ?? "表示名";
                dgvMonitoredServices.Columns["NotificationEnabled"].HeaderText = _localizationService.GetString("MainForm_ColumnNotification") ?? "通知";
                dgvMonitoredServices.Columns["Status"].HeaderText = _localizationService.GetString("MainForm_ColumnStatus") ?? "状態";
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
