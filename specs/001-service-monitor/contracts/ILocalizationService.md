# ILocalizationService Contract

**Feature**: 001-service-monitor  
**Date**: 2025-11-05  
**Purpose**: Manage UI localization and runtime language switching

## Interface Definition

```csharp
public interface ILocalizationService
{
    /// <summary>
    /// Gets the currently active UI language.
    /// </summary>
    /// <returns>"ja" for Japanese, "en" for English</returns>
    string CurrentLanguage { get; }
    
    /// <summary>
    /// Gets the list of supported language codes.
    /// </summary>
    /// <returns>Array containing "ja" and "en"</returns>
    string[] SupportedLanguages { get; }
    
    /// <summary>
    /// Detects the default language based on OS culture settings.
    /// </summary>
    /// <returns>"ja" if OS is Japanese, "en" otherwise</returns>
    string DetectDefaultLanguage();
    
    /// <summary>
    /// Changes the active UI language and updates all open forms.
    /// </summary>
    /// <param name="languageCode">Must be "ja" or "en"</param>
    /// <returns>Result indicating success or validation error</returns>
    /// <exception cref="ArgumentException">Thrown if languageCode is not supported</exception>
    Result SetLanguage(string languageCode);
    
    /// <summary>
    /// Applies localized resources to a Windows Forms Form.
    /// </summary>
    /// <param name="form">The form to localize</param>
    void ApplyResourcesTo(Form form);
    
    /// <summary>
    /// Gets a localized string by resource key.
    /// </summary>
    /// <param name="key">Resource key (e.g., "MainForm_Title")</param>
    /// <returns>Localized string, or key itself if not found</returns>
    string GetString(string key);
    
    /// <summary>
    /// Gets a localized formatted string with parameters.
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized and formatted string</returns>
    string GetFormattedString(string key, params object[] args);
}
```

## Usage Examples

### Initialization

```csharp
// In Program.cs
var localizationService = new LocalizationService();
localizationService.SetLanguage(config.UiLanguage);
```

### Language Detection on First Launch

```csharp
string defaultLanguage = localizationService.DetectDefaultLanguage();
config.UiLanguage = defaultLanguage; // "ja" or "en"
```

### Runtime Language Switching

```csharp
// In SettingsForm language dropdown change handler
private void LanguageDropdown_SelectedIndexChanged(object sender, EventArgs e)
{
    string selectedLanguage = languageDropdown.SelectedValue.ToString();
    
    var result = _localizationService.SetLanguage(selectedLanguage);
    if (result.IsSuccess)
    {
        // Update all open forms
        foreach (Form form in Application.OpenForms)
        {
            _localizationService.ApplyResourcesTo(form);
        }
        
        // Save to config
        _configManager.UpdateLanguage(selectedLanguage);
    }
    else
    {
        MessageBox.Show(result.Error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```

### Getting Localized Strings

```csharp
// Simple string
string title = _localizationService.GetString("MainForm_Title");
// Returns: "Service Watcher" (EN) or "Service Watcher" (JA)

// Formatted string
string message = _localizationService.GetFormattedString(
    "ServiceStoppedNotification", 
    "Windows Update", 
    DateTime.Now
);
// Returns: "Windows Update stopped at 14:35:22" (EN)
// Returns: "Windows Update が 14:35:22 に停止しました" (JA)
```

### Applying Resources to Form

```csharp
public class MainForm : Form
{
    private readonly ILocalizationService _localizationService;
    
    public MainForm(ILocalizationService localizationService)
    {
        InitializeComponent();
        _localizationService = localizationService;
        
        // Apply localized resources on load
        _localizationService.ApplyResourcesTo(this);
    }
}
```

## Implementation Notes

### Resource Files Structure

```
Resources/
├── Strings.resx           # Default (fallback) strings
├── Strings.ja.resx        # Japanese strings
└── Strings.en.resx        # English strings
```

### Key Naming Convention

- Form titles: `{FormName}_Title`
- Buttons: `{FormName}_{ButtonName}_Text`
- Labels: `{FormName}_{LabelName}_Text`
- Messages: `{MessageType}_Message`

Example:
```xml
<!-- Strings.en.resx -->
<data name="MainForm_Title">
  <value>Service Watcher</value>
</data>
<data name="MainForm_StartButton_Text">
  <value>Start Monitoring</value>
</data>
<data name="ServiceStoppedNotification">
  <value>{0} stopped at {1:HH:mm:ss}</value>
</data>

<!-- Strings.ja.resx -->
<data name="MainForm_Title">
  <value>Service Watcher</value>
</data>
<data name="MainForm_StartButton_Text">
  <value>監視開始</value>
</data>
<data name="ServiceStoppedNotification">
  <value>{0} が {1:HH:mm:ss} に停止しました</value>
</data>
```

### Implementation Class

```csharp
public class LocalizationService : ILocalizationService
{
    private CultureInfo _currentCulture;
    private ResourceManager _resourceManager;
    
    public string CurrentLanguage => _currentCulture.TwoLetterISOLanguageName;
    
    public string[] SupportedLanguages => new[] { "ja", "en" };
    
    public LocalizationService()
    {
        _resourceManager = new ResourceManager(typeof(Resources.Strings));
        _currentCulture = CultureInfo.CurrentUICulture;
    }
    
    public string DetectDefaultLanguage()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return culture == "ja" ? "ja" : "en";
    }
    
    public Result SetLanguage(string languageCode)
    {
        if (!SupportedLanguages.Contains(languageCode))
        {
            return Result.Failure($"Unsupported language: {languageCode}");
        }
        
        _currentCulture = new CultureInfo(languageCode);
        Thread.CurrentThread.CurrentUICulture = _currentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;
        
        return Result.Success();
    }
    
    public void ApplyResourcesTo(Form form)
    {
        ComponentResourceManager resources = new ComponentResourceManager(form.GetType());
        resources.ApplyResources(form, "$this", _currentCulture);
        
        // Recursively apply to all controls
        ApplyResourcesToControls(form.Controls, resources);
    }
    
    private void ApplyResourcesToControls(Control.ControlCollection controls, ComponentResourceManager resources)
    {
        foreach (Control control in controls)
        {
            resources.ApplyResources(control, control.Name, _currentCulture);
            if (control.HasChildren)
            {
                ApplyResourcesToControls(control.Controls, resources);
            }
        }
    }
    
    public string GetString(string key)
    {
        return _resourceManager.GetString(key, _currentCulture) ?? key;
    }
    
    public string GetFormattedString(string key, params object[] args)
    {
        string format = GetString(key);
        return string.Format(format, args);
    }
}
```

## Testing

### Unit Tests

```csharp
[Fact]
public void DetectDefaultLanguage_ShouldReturnJapanese_WhenOSIsJapanese()
{
    // Arrange
    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ja-JP");
    var service = new LocalizationService();
    
    // Act
    string language = service.DetectDefaultLanguage();
    
    // Assert
    Assert.Equal("ja", language);
}

[Fact]
public void DetectDefaultLanguage_ShouldReturnEnglish_WhenOSIsNonJapanese()
{
    // Arrange
    Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
    var service = new LocalizationService();
    
    // Act
    string language = service.DetectDefaultLanguage();
    
    // Assert
    Assert.Equal("en", language);
}

[Fact]
public void SetLanguage_ShouldReturnFailure_WhenLanguageNotSupported()
{
    // Arrange
    var service = new LocalizationService();
    
    // Act
    var result = service.SetLanguage("fr");
    
    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Unsupported language", result.Error);
}

[Fact]
public void SetLanguage_ShouldUpdateCurrentCulture_WhenLanguageIsValid()
{
    // Arrange
    var service = new LocalizationService();
    
    // Act
    var result = service.SetLanguage("en");
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("en", service.CurrentLanguage);
}
```

## Performance Requirements

- **Language switch**: <1 second for all open forms (SC-008)
- **GetString**: <10ms per call (cached by ResourceManager)
- **ApplyResourcesTo**: <100ms per form with <50 controls

## Dependencies

- System.Globalization.CultureInfo
- System.Resources.ResourceManager
- System.ComponentModel.ComponentResourceManager
- System.Windows.Forms.Form

## Related Documents

- [FR-013 to FR-018](../spec.md#機能要件-functional-requirements) - i18n functional requirements
- [US4](../spec.md#ユーザーストーリー-user-stories) - Language switching user story
- [Scenario 11](../quickstart.md#scenario-11-language-switching-i18n) - Testing guide
