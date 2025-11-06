using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Interface for managing UI localization and runtime language switching.
/// </summary>
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
    /// Changes the active UI language and updates thread culture.
    /// </summary>
    /// <param name="languageCode">Must be "ja" or "en"</param>
    /// <returns>Result indicating success or validation error</returns>
    Result<bool> SetLanguage(string languageCode);
    
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
