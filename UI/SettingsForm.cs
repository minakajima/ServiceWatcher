using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Services;
using ServiceWatcher.Utils;
using System.Text.Json;

namespace ServiceWatcher.UI;

/// <summary>
/// Settings form for configuring application options.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class SettingsForm : Form
{
    /// <summary>
    /// Helper class for language dropdown items.
    /// </summary>
    private class LanguageItem
    {
        public string Code { get; }
        public string DisplayName { get; }

        public LanguageItem(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        public override string ToString() => DisplayName;
    }

    private readonly ILogger<SettingsForm> _logger;
    private readonly ILocalizationService? _localizationService;
    private NumericUpDown nudMonitoringInterval = null!;
    private NumericUpDown nudNotificationDisplayTime = null!;
    private ComboBox cmbLanguage = null!;
    private CheckBox chkStartMinimized = null!;
    private CheckBox chkAutoStartMonitoring = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Label lblMonitoringInterval = null!;
    private Label lblNotificationDisplayTime = null!;
    private Label lblLanguage = null!;
    private Label lblSeconds1 = null!;
    private Label lblSeconds2 = null!;
    private GroupBox grpGeneral = null!;
    private GroupBox grpStartup = null!;

    // Current settings
    private int _monitoringInterval;
    private int _notificationDisplayTime;
    private string _uiLanguage = "ja";
    private bool _startMinimized;
    private bool _autoStartMonitoring;

    // Original language before changes
    private string _originalLanguage = "ja";

    public SettingsForm(ILogger<SettingsForm> logger, ILocalizationService? localizationService = null)
    {
        _logger = logger;
        _localizationService = localizationService;
        InitializeComponent();
        ApplyLocalization();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "設定 - ServiceWatcher";
        this.Size = new Size(500, 440);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // General settings group
        grpGeneral = new GroupBox
        {
            Text = "一般設定",
            Location = new Point(12, 12),
            Size = new Size(460, 160)
        };
        this.Controls.Add(grpGeneral);

        lblLanguage = new Label
        {
            Text = "言語:",
            Location = new Point(15, 30),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblLanguage);

        cmbLanguage = new ComboBox
        {
            Location = new Point(180, 27),
            Size = new Size(150, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbLanguage.Items.Add(new LanguageItem("ja", "日本語"));
        cmbLanguage.Items.Add(new LanguageItem("en", "English"));
        cmbLanguage.SelectedIndex = 0;
        cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
        grpGeneral.Controls.Add(cmbLanguage);

        lblMonitoringInterval = new Label
        {
            Text = "監視間隔:",
            Location = new Point(15, 65),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblMonitoringInterval);

        nudMonitoringInterval = new NumericUpDown
        {
            Location = new Point(180, 62),
            Size = new Size(80, 23),
            Minimum = 1,
            Maximum = 3600,
            Value = 5
        };
        grpGeneral.Controls.Add(nudMonitoringInterval);

        lblSeconds1 = new Label
        {
            Text = "秒",
            Location = new Point(265, 65),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblSeconds1);

        lblNotificationDisplayTime = new Label
        {
            Text = "通知表示時間:",
            Location = new Point(15, 100),
            AutoSize = true
        };
        grpGeneral.Controls.Add(lblNotificationDisplayTime);

        nudNotificationDisplayTime = new NumericUpDown
        {
            Location = new Point(180, 97),
            Size = new Size(80, 23),
            Minimum = 0,
            Maximum = 300,
            Value = 30
        };
        grpGeneral.Controls.Add(nudNotificationDisplayTime);

        lblSeconds2 = new Label
        {
            Text = "秒（0=手動で閉じるまで表示）",
            Location = new Point(265, 100),
            AutoSize = true,
            MaximumSize = new Size(220, 0)
        };
        grpGeneral.Controls.Add(lblSeconds2);

        // Startup settings group
        grpStartup = new GroupBox
        {
            Text = "起動時設定",
            Location = new Point(12, 185),
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
            Location = new Point(285, 360),
            Size = new Size(90, 30),
            DialogResult = DialogResult.OK
        };
        btnSave.Click += BtnSave_Click;
        this.Controls.Add(btnSave);

        btnCancel = new Button
        {
            Text = "キャンセル",
            Location = new Point(382, 360),
            Size = new Size(90, 30),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.Click += BtnCancel_Click;
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    /// <summary>
    /// Applies localized text to all UI elements.
    /// </summary>
    private void ApplyLocalization()
    {
        if (_localizationService == null)
        {
            return;
        }

        // Form title
        this.Text = _localizationService.GetString("SettingsForm_Title") ?? "設定 - ServiceWatcher";

        // Group boxes
        grpGeneral.Text = _localizationService.GetString("SettingsForm_GeneralGroup") ?? "一般設定";
        grpStartup.Text = _localizationService.GetString("SettingsForm_StartupGroup") ?? "起動時設定";

        // Labels
        lblLanguage.Text = _localizationService.GetString("SettingsForm_LanguageLabel") ?? "言語:";
        lblMonitoringInterval.Text = _localizationService.GetString("SettingsForm_MonitoringIntervalLabel") ?? "監視間隔:";
        lblNotificationDisplayTime.Text = _localizationService.GetString("SettingsForm_NotificationDisplayTimeLabel") ?? "通知表示時間:";
        lblSeconds1.Text = _localizationService.GetString("SettingsForm_SecondsLabel") ?? "秒";
        lblSeconds2.Text = _localizationService.GetString("SettingsForm_SecondsWithNote") ?? "秒（0=手動で閉じるまで表示）";

        // Checkboxes
        chkStartMinimized.Text = _localizationService.GetString("SettingsForm_StartMinimizedCheckbox") ?? "最小化して起動";
        chkAutoStartMonitoring.Text = _localizationService.GetString("SettingsForm_AutoStartMonitoringCheckbox") ?? "起動時に自動的に監視を開始";

        // Buttons
        btnSave.Text = _localizationService.GetString("SettingsForm_SaveButton") ?? "保存";
        btnCancel.Text = _localizationService.GetString("SettingsForm_CancelButton") ?? "キャンセル";
    }

    /// <summary>
    /// Handles language dropdown selection change.
    /// </summary>
    private void CmbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_localizationService == null || cmbLanguage.SelectedItem is not LanguageItem selectedLanguage)
        {
            return;
        }

        // Set language
        var result = _localizationService.SetLanguage(selectedLanguage.Code);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to change language: {Error}", result.Error);
            return;
        }

        // Apply to all open forms
        foreach (Form form in Application.OpenForms)
        {
            if (form is MainForm mainForm)
            {
                // MainForm has its own ApplyLocalization method
                var applyMethod = mainForm.GetType().GetMethod("ApplyLocalization", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                applyMethod?.Invoke(mainForm, null);
            }
        }

        // Apply to this form
        ApplyLocalization();

        _logger.LogInformation("Language changed to: {Language}", selectedLanguage.Code);
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

            if (root.TryGetProperty("uiLanguage", out var languageProp))
            {
                _uiLanguage = languageProp.GetString() ?? "ja";
                _originalLanguage = _uiLanguage; // Save original language
                var langIndex = _uiLanguage == "en" ? 1 : 0;
                cmbLanguage.SelectedIndex = langIndex;
            }

            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            var message = _localizationService?.GetFormattedString("SettingsForm_LoadFailedMessage", ex.Message) 
                ?? $"設定の読み込みに失敗しました: {ex.Message}\n\nデフォルト値を使用します。";
            MessageBox.Show(
                message,
                _localizationService?.GetString("SettingsForm_WarningTitle") ?? "警告",
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
            var uiLanguage = (cmbLanguage.SelectedItem as LanguageItem)?.Code ?? "ja";
            var startMinimized = chkStartMinimized.Checked;
            var autoStartMonitoring = chkAutoStartMonitoring.Checked;

            // Validate
            var intervalValidation = Utils.ConfigurationValidator.ValidateMonitoringInterval(monitoringInterval);
            if (!intervalValidation.IsValid)
            {
                MessageBox.Show(
                    string.Join("\n", intervalValidation.Errors),
                    _localizationService?.GetString("SettingsForm_ValidationError") ?? "検証エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var displayTimeValidation = Utils.ConfigurationValidator.ValidateNotificationDisplayTime(notificationDisplayTime);
            if (!displayTimeValidation.IsValid)
            {
                MessageBox.Show(
                    string.Join("\n", displayTimeValidation.Errors),
                    _localizationService?.GetString("SettingsForm_ValidationError") ?? "検証エラー",
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
                    _localizationService?.GetString("Error_Title") ?? "エラー",
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
                        validateResult.Error + "\n\n" + (_localizationService?.GetString("SettingsForm_RestorePrompt") ?? "バックアップから復元しますか？"),
                        _localizationService?.GetString("SettingsForm_ConfigErrorTitle") ?? "設定ファイルエラー",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        var restoreResult = ConfigurationHelper.RestoreFromBackup(configPath, _logger);
                        if (!restoreResult.IsSuccess)
                        {
                            MessageBox.Show(restoreResult.Error, 
                                _localizationService?.GetString("SettingsForm_RestoreFailed") ?? "復元失敗", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(createResult.Error, 
                        _localizationService?.GetString("Error_Title") ?? "エラー", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config == null)
            {
                _logger.LogError("Failed to deserialize config.json");
                MessageBox.Show(
                    _localizationService?.GetString("SettingsForm_LoadFailedShort") ?? "設定ファイルの読み込みに失敗しました。",
                    _localizationService?.GetString("Error_Title") ?? "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Update values
            config["monitoringIntervalSeconds"] = JsonSerializer.SerializeToElement(monitoringInterval);
            config["notificationDisplayTimeSeconds"] = JsonSerializer.SerializeToElement(notificationDisplayTime);
            config["uiLanguage"] = JsonSerializer.SerializeToElement(uiLanguage);
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
                _localizationService?.GetString("SettingsForm_SaveSuccessMessage") ?? 
                "設定を保存しました。\n\n変更を反映するにはアプリケーションを再起動してください。",
                _localizationService?.GetString("SettingsForm_SuccessTitle") ?? "成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            var message = _localizationService?.GetFormattedString("SettingsForm_SaveFailedMessage", ex.Message) 
                ?? $"設定の保存に失敗しました: {ex.Message}";
            MessageBox.Show(
                message,
                _localizationService?.GetString("Error_Title") ?? "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Handles cancel button click - restores original language.
    /// </summary>
    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        // Restore original language if it was changed
        var currentLanguage = (cmbLanguage.SelectedItem as LanguageItem)?.Code ?? "ja";
        if (currentLanguage != _originalLanguage && _localizationService != null)
        {
            _logger.LogInformation($"Restoring language from {currentLanguage} to {_originalLanguage}");
            
            // Restore language in service
            var result = _localizationService.SetLanguage(_originalLanguage);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to restore language: {Error}", result.Error);
            }
            else
            {
                // Apply to all open forms
                foreach (Form form in Application.OpenForms)
                {
                    if (form is MainForm mainForm)
                    {
                        mainForm.ApplyLocalization();
                    }
                    else if (form is ServiceListForm serviceListForm)
                    {
                        serviceListForm.ApplyLocalization();
                    }
                }
            }
        }

        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}
