using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Models;
using ServiceWatcher.Services;
using ServiceWatcher.Utils;
using System.Text.Json;

namespace ServiceWatcher.UI;

/// <summary>
/// Form for browsing and selecting Windows services to monitor.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class ServiceListForm : Form
{
    private readonly IServiceMonitor _serviceMonitor;
    private readonly ILogger<ServiceListForm> _logger;
    private DataGridView dgvAllServices = null!;
    private DataGridView dgvMonitoredServices = null!;
    private TextBox txtSearch = null!;
    private Button btnAddService = null!;
    private Button btnRemoveService = null!;
    private Button btnClose = null!;
    private Label lblSearch = null!;
    private Label lblAllServices = null!;
    private Label lblMonitoredServices = null!;
    private List<ServiceInfo> _allServices;
    private List<MonitoredService> _monitoredServices;

    /// <summary>
    /// Service information for display.
    /// </summary>
    private class ServiceInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StartType { get; set; } = string.Empty;
    }

    public ServiceListForm(IServiceMonitor serviceMonitor, ILogger<ServiceListForm> logger)
    {
        _serviceMonitor = serviceMonitor;
        _logger = logger;
        _allServices = new List<ServiceInfo>();
        _monitoredServices = new List<MonitoredService>();
        
        InitializeComponent();
        LoadAllServices();
        LoadMonitoredServices();
    }

    private void InitializeComponent()
    {
        this.Text = "サービス一覧 - ServiceWatcher";
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // Search controls
        lblSearch = new Label
        {
            Text = "検索:",
            Location = new Point(12, 15),
            AutoSize = true
        };
        this.Controls.Add(lblSearch);

        txtSearch = new TextBox
        {
            Location = new Point(60, 12),
            Size = new Size(300, 23),
            PlaceholderText = "サービス名または表示名で検索..."
        };
        txtSearch.TextChanged += TxtSearch_TextChanged;
        this.Controls.Add(txtSearch);

        // All services grid
        lblAllServices = new Label
        {
            Text = "利用可能なサービス:",
            Location = new Point(12, 45),
            AutoSize = true
        };
        this.Controls.Add(lblAllServices);

        dgvAllServices = new DataGridView
        {
            Location = new Point(12, 70),
            Size = new Size(960, 200),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvAllServices.DoubleClick += DgvAllServices_DoubleClick;
        this.Controls.Add(dgvAllServices);

        // Action buttons
        btnAddService = new Button
        {
            Text = "監視対象に追加 ▼",
            Location = new Point(420, 280),
            Size = new Size(150, 30)
        };
        btnAddService.Click += BtnAddService_Click;
        this.Controls.Add(btnAddService);

        // Monitored services grid
        lblMonitoredServices = new Label
        {
            Text = "監視対象サービス:",
            Location = new Point(12, 320),
            AutoSize = true
        };
        this.Controls.Add(lblMonitoredServices);

        dgvMonitoredServices = new DataGridView
        {
            Location = new Point(12, 345),
            Size = new Size(960, 150),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvMonitoredServices.DoubleClick += DgvMonitoredServices_DoubleClick;
        this.Controls.Add(dgvMonitoredServices);

        // Remove button
        btnRemoveService = new Button
        {
            Text = "監視対象から削除",
            Location = new Point(420, 505),
            Size = new Size(150, 30)
        };
        btnRemoveService.Click += BtnRemoveService_Click;
        this.Controls.Add(btnRemoveService);

        // Close button
        btnClose = new Button
        {
            Text = "閉じる",
            Location = new Point(885, 525),
            Size = new Size(87, 30),
            DialogResult = DialogResult.OK
        };
        this.Controls.Add(btnClose);
        this.AcceptButton = btnClose;
    }

    /// <summary>
    /// Load all Windows services from the system.
    /// </summary>
    private void LoadAllServices()
    {
        try
        {
            var services = ServiceController.GetServices();
            _allServices = services.Select(s => new ServiceInfo
            {
                ServiceName = s.ServiceName,
                DisplayName = s.DisplayName,
                Status = s.Status.ToString(),
                StartType = s.StartType.ToString()
            }).OrderBy(s => s.DisplayName).ToList();

            UpdateAllServicesGrid(_allServices);
            _logger.LogInformation($"Loaded {_allServices.Count} services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load services");
            MessageBox.Show(
                $"サービス一覧の読み込みに失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Load currently monitored services.
    /// </summary>
    private void LoadMonitoredServices()
    {
        _monitoredServices = SimpleConfigLoader.LoadServices();
        UpdateMonitoredServicesGrid();
    }

    /// <summary>
    /// Update the all services grid with filtered data.
    /// </summary>
    private void UpdateAllServicesGrid(List<ServiceInfo> services)
    {
        dgvAllServices.DataSource = null;
        dgvAllServices.DataSource = services;
        
        if (dgvAllServices.Columns.Count > 0)
        {
            dgvAllServices.Columns["ServiceName"].HeaderText = "サービス名";
            dgvAllServices.Columns["DisplayName"].HeaderText = "表示名";
            dgvAllServices.Columns["Status"].HeaderText = "状態";
            dgvAllServices.Columns["StartType"].HeaderText = "スタートアップの種類";
        }
    }

    /// <summary>
    /// Update the monitored services grid.
    /// </summary>
    private void UpdateMonitoredServicesGrid()
    {
        dgvMonitoredServices.DataSource = null;
        dgvMonitoredServices.DataSource = _monitoredServices.Select(m => new
        {
            ServiceName = m.ServiceName,
            DisplayName = m.DisplayName,
            NotificationEnabled = m.NotificationEnabled ? "有効" : "無効",
            LastStatus = m.LastKnownStatus.ToString()
        }).ToList();

        if (dgvMonitoredServices.Columns.Count > 0)
        {
            dgvMonitoredServices.Columns["ServiceName"].HeaderText = "サービス名";
            dgvMonitoredServices.Columns["DisplayName"].HeaderText = "表示名";
            dgvMonitoredServices.Columns["NotificationEnabled"].HeaderText = "通知";
            dgvMonitoredServices.Columns["LastStatus"].HeaderText = "状態";
        }
    }

    /// <summary>
    /// Filter services based on search text.
    /// </summary>
    private void TxtSearch_TextChanged(object? sender, EventArgs e)
    {
        var searchText = txtSearch.Text.Trim().ToLowerInvariant();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            UpdateAllServicesGrid(_allServices);
            return;
        }

        var filtered = _allServices.Where(s =>
            s.ServiceName.ToLowerInvariant().Contains(searchText) ||
            s.DisplayName.ToLowerInvariant().Contains(searchText)
        ).ToList();

        UpdateAllServicesGrid(filtered);
        _logger.LogInformation($"Filtered to {filtered.Count} services matching '{searchText}'");
    }

    /// <summary>
    /// Add selected service to monitoring.
    /// </summary>
    private async void BtnAddService_Click(object? sender, EventArgs e)
    {
        if (dgvAllServices.SelectedRows.Count == 0)
        {
            MessageBox.Show("サービスを選択してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedRow = dgvAllServices.SelectedRows[0];
        var serviceName = selectedRow.Cells["ServiceName"].Value?.ToString();
        var displayName = selectedRow.Cells["DisplayName"].Value?.ToString();

        if (string.IsNullOrEmpty(serviceName))
            return;

        // Check if already monitored
        if (_monitoredServices.Any(m => m.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                $"サービス '{displayName}' は既に監視対象に含まれています。",
                "情報",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Create new monitored service
        var newService = new MonitoredService
        {
            ServiceName = serviceName,
            DisplayName = displayName ?? serviceName,
            NotificationEnabled = true
        };

        // Save to config
        if (await SaveMonitoredService(newService))
        {
            _monitoredServices.Add(newService);
            UpdateMonitoredServicesGrid();
            
            MessageBox.Show(
                $"サービス '{displayName}' を監視対象に追加しました。",
                "成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _logger.LogInformation($"Added service to monitoring: {serviceName}");
        }
    }

    /// <summary>
    /// Remove selected service from monitoring.
    /// </summary>
    private async void BtnRemoveService_Click(object? sender, EventArgs e)
    {
        if (dgvMonitoredServices.SelectedRows.Count == 0)
        {
            MessageBox.Show("監視対象サービスを選択してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedRow = dgvMonitoredServices.SelectedRows[0];
        var serviceName = selectedRow.Cells["ServiceName"].Value?.ToString();
        var displayName = selectedRow.Cells["DisplayName"].Value?.ToString();

        if (string.IsNullOrEmpty(serviceName))
            return;

        var result = MessageBox.Show(
            $"サービス '{displayName}' を監視対象から削除しますか？",
            "確認",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        // Remove from config
        if (await RemoveMonitoredService(serviceName))
        {
            _monitoredServices.RemoveAll(m => m.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            UpdateMonitoredServicesGrid();

            MessageBox.Show(
                $"サービス '{displayName}' を監視対象から削除しました。",
                "成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _logger.LogInformation($"Removed service from monitoring: {serviceName}");
        }
    }

    /// <summary>
    /// Double-click on all services grid to add.
    /// </summary>
    private void DgvAllServices_DoubleClick(object? sender, EventArgs e)
    {
        BtnAddService_Click(sender, e);
    }

    /// <summary>
    /// Double-click on monitored services grid to remove.
    /// </summary>
    private void DgvMonitoredServices_DoubleClick(object? sender, EventArgs e)
    {
        BtnRemoveService_Click(sender, e);
    }

    /// <summary>
    /// Save a new monitored service to config.json.
    /// </summary>
    private async Task<bool> SaveMonitoredService(MonitoredService service)
    {
        try
        {
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            
            // Check if file is writable
            var writableResult = ConfigurationHelper.IsConfigWritable(configPath, _logger);
            if (!writableResult.IsSuccess)
            {
                MessageBox.Show(writableResult.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Validate config file
            if (File.Exists(configPath))
            {
                var validateResult = ConfigurationHelper.ValidateAndRepairConfig(configPath, _logger);
                if (!validateResult.IsSuccess)
                {
                    var result = MessageBox.Show(
                        validateResult.Error + "\n\nバックアップから復元しますか？",
                        "設定ファイルエラー",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        var restoreResult = ConfigurationHelper.RestoreFromBackup(configPath, _logger);
                        if (!restoreResult.IsSuccess)
                        {
                            MessageBox.Show(restoreResult.Error, "復元失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Create default config
                var createResult = ConfigurationHelper.CreateDefaultConfig(configPath, _logger);
                if (!createResult.IsSuccess)
                {
                    MessageBox.Show(createResult.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            // Create backup before saving
            if (File.Exists(configPath))
            {
                ConfigurationHelper.CreateBackup(configPath, _logger);
            }

            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config == null)
            {
                _logger.LogError("Failed to deserialize config.json");
                return false;
            }

            // Get existing services
            var services = new List<MonitoredService>();
            if (config.ContainsKey("monitoredServices"))
            {
                services = JsonSerializer.Deserialize<List<MonitoredService>>(config["monitoredServices"].GetRawText()) ?? new List<MonitoredService>();
            }

            // Add new service
            services.Add(service);
            config["monitoredServices"] = JsonSerializer.SerializeToElement(services);

            // Update lastModified
            config["lastModified"] = JsonSerializer.SerializeToElement(DateTime.Now);

            // Save back to file
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var updatedJson = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(configPath, updatedJson);

            _logger.LogInformation($"Saved service to config: {service.ServiceName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save monitored service");
            MessageBox.Show(
                $"設定の保存に失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    /// <summary>
    /// Remove a monitored service from config.json.
    /// </summary>
    private async Task<bool> RemoveMonitoredService(string serviceName)
    {
        try
        {
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            
            // Check if file is writable
            var writableResult = ConfigurationHelper.IsConfigWritable(configPath, _logger);
            if (!writableResult.IsSuccess)
            {
                MessageBox.Show(writableResult.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!File.Exists(configPath))
            {
                MessageBox.Show("config.jsonファイルが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Create backup before modifying
            ConfigurationHelper.CreateBackup(configPath, _logger);

            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config == null)
            {
                _logger.LogError("Failed to deserialize config.json");
                return false;
            }

            // Get existing services
            var services = new List<MonitoredService>();
            if (config.ContainsKey("monitoredServices"))
            {
                services = JsonSerializer.Deserialize<List<MonitoredService>>(config["monitoredServices"].GetRawText()) ?? new List<MonitoredService>();
            }

            // Remove service
            services.RemoveAll(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            config["monitoredServices"] = JsonSerializer.SerializeToElement(services);

            // Update lastModified
            config["lastModified"] = JsonSerializer.SerializeToElement(DateTime.Now);

            // Save back to file
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var updatedJson = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(configPath, updatedJson);

            _logger.LogInformation($"Removed service from config: {serviceName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove monitored service");
            MessageBox.Show(
                $"設定の削除に失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }
}
