using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Utils;
using System.Text.Json;

namespace ServiceWatcher.UI;

/// <summary>
/// Settings form for configuring application options.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class SettingsForm : Form
{
    private readonly ILogger<SettingsForm> _logger;
    private NumericUpDown nudMonitoringInterval = null!;
    private NumericUpDown nudNotificationDisplayTime = null!;
    private CheckBox chkStartMinimized = null!;
    private CheckBox chkAutoStartMonitoring = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Label lblMonitoringInterval = null!;
    private Label lblNotificationDisplayTime = null!;
    private Label lblSeconds1 = null!;
    private Label lblSeconds2 = null!;
    private GroupBox grpGeneral = null!;
    private GroupBox grpStartup = null!;

    // Current settings
    private int _monitoringInterval;
    private int _notificationDisplayTime;
    private bool _startMinimized;
    private bool _autoStartMonitoring;

    public SettingsForm(ILogger<SettingsForm> logger)
    {
        _logger = logger;
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "設定 - ServiceWatcher";
        this.Size = new Size(500, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // General settings group
        grpGeneral = new GroupBox
        {
            Text = "一般設定",
            Location = new Point(12, 12),
            Size = new Size(460, 120)
        };
        this.Controls.Add(grpGeneral);

        lblMonitoringInterval = new Label
        {
            Text = "監視間隔:",
            Location = new Point(15, 30),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblMonitoringInterval);

        nudMonitoringInterval = new NumericUpDown
        {
            Location = new Point(180, 27),
            Size = new Size(80, 23),
            Minimum = 1,
            Maximum = 3600,
            Value = 5
        };
        grpGeneral.Controls.Add(nudMonitoringInterval);

        lblSeconds1 = new Label
        {
            Text = "秒",
            Location = new Point(265, 30),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblSeconds1);

        lblNotificationDisplayTime = new Label
        {
            Text = "通知表示時間:",
            Location = new Point(15, 65),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblNotificationDisplayTime);

        nudNotificationDisplayTime = new NumericUpDown
        {
            Location = new Point(180, 62),
            Size = new Size(80, 23),
            Minimum = 0,
            Maximum = 300,
            Value = 30
        };
        grpGeneral.Controls.Add(nudNotificationDisplayTime);

        lblSeconds2 = new Label
        {
            Text = "秒（0=手動で閉じるまで表示）",
            Location = new Point(265, 65),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblSeconds2);

        // Startup settings group
        grpStartup = new GroupBox
        {
            Text = "起動時設定",
            Location = new Point(12, 145),
            Size = new Size(460, 100)
        };
        this.Controls.Add(grpStartup);

        chkStartMinimized = new CheckBox
        {
            Text = "最小化して起動",
            Location = new Point(15, 30),
            AutoSize = true
        };
        grpStartup.Controls.Add(chkStartMinimized);

        chkAutoStartMonitoring = new CheckBox
        {
            Text = "起動時に自動的に監視を開始",
            Location = new Point(15, 60),
            AutoSize = true
        };
        grpStartup.Controls.Add(chkAutoStartMonitoring);

        // Buttons
        btnSave = new Button
        {
            Text = "保存",
            Location = new Point(285, 320),
            Size = new Size(90, 30),
            DialogResult = DialogResult.OK
        };
        btnSave.Click += BtnSave_Click;
        this.Controls.Add(btnSave);

        btnCancel = new Button
        {
            Text = "キャンセル",
            Location = new Point(382, 320),
            Size = new Size(90, 30),
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    /// <summary>
    /// Load current settings from config.json.
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Config file not found, using defaults");
                return;
            }

            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Load values
            if (root.TryGetProperty("monitoringIntervalSeconds", out var intervalProp))
            {
                _monitoringInterval = intervalProp.GetInt32();
                nudMonitoringInterval.Value = Math.Clamp(_monitoringInterval, 1, 3600);
            }

            if (root.TryGetProperty("notificationDisplayTimeSeconds", out var displayTimeProp))
            {
                _notificationDisplayTime = displayTimeProp.GetInt32();
                nudNotificationDisplayTime.Value = Math.Clamp(_notificationDisplayTime, 0, 300);
            }

            if (root.TryGetProperty("startMinimized", out var startMinProp))
            {
                _startMinimized = startMinProp.GetBoolean();
                chkStartMinimized.Checked = _startMinimized;
            }

            if (root.TryGetProperty("autoStartMonitoring", out var autoStartProp))
            {
                _autoStartMonitoring = autoStartProp.GetBoolean();
                chkAutoStartMonitoring.Checked = _autoStartMonitoring;
            }

            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            MessageBox.Show(
                $"設定の読み込みに失敗しました: {ex.Message}\n\nデフォルト値を使用します。",
                "警告",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Save settings to config.json.
    /// </summary>
    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // Get values from controls
            var monitoringInterval = (int)nudMonitoringInterval.Value;
            var notificationDisplayTime = (int)nudNotificationDisplayTime.Value;
            var startMinimized = chkStartMinimized.Checked;
            var autoStartMonitoring = chkAutoStartMonitoring.Checked;

            // Validate
            var intervalValidation = ConfigurationValidator.ValidateMonitoringInterval(monitoringInterval);
            if (!intervalValidation.IsValid)
            {
                MessageBox.Show(
                    string.Join("\n", intervalValidation.Errors),
                    "検証エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var displayTimeValidation = ConfigurationValidator.ValidateNotificationDisplayTime(notificationDisplayTime);
            if (!displayTimeValidation.IsValid)
            {
                MessageBox.Show(
                    string.Join("\n", displayTimeValidation.Errors),
                    "検証エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Save to config.json
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            
            // Check if file is writable
            var writableResult = ConfigurationHelper.IsConfigWritable(configPath, _logger);
            if (!writableResult.IsSuccess)
            {
                MessageBox.Show(
                    writableResult.Error,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Validate existing config
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
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                // Create backup before saving
                ConfigurationHelper.CreateBackup(configPath, _logger);
            }
            else
            {
                // Create default config
                var createResult = ConfigurationHelper.CreateDefaultConfig(configPath, _logger);
                if (!createResult.IsSuccess)
                {
                    MessageBox.Show(createResult.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config == null)
            {
                _logger.LogError("Failed to deserialize config.json");
                MessageBox.Show(
                    "設定ファイルの読み込みに失敗しました。",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Update values
            config["monitoringIntervalSeconds"] = JsonSerializer.SerializeToElement(monitoringInterval);
            config["notificationDisplayTimeSeconds"] = JsonSerializer.SerializeToElement(notificationDisplayTime);
            config["startMinimized"] = JsonSerializer.SerializeToElement(startMinimized);
            config["autoStartMonitoring"] = JsonSerializer.SerializeToElement(autoStartMonitoring);
            config["lastModified"] = JsonSerializer.SerializeToElement(DateTime.Now);

            // Save
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var updatedJson = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(configPath, updatedJson);

            _logger.LogInformation("Settings saved successfully");

            MessageBox.Show(
                "設定を保存しました。\n\n変更を反映するにはアプリケーションを再起動してください。",
                "成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            MessageBox.Show(
                $"設定の保存に失敗しました: {ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
