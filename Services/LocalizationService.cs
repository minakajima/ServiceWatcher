using System.ComponentModel;
using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Manages UI localization and runtime language switching using .resx resource files.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private CultureInfo _currentCulture;
    private ResourceManager? _resourceManager;
    
    /// <inheritdoc />
    public string CurrentLanguage => _currentCulture.TwoLetterISOLanguageName;
    
    /// <inheritdoc />
    public string[] SupportedLanguages => new[] { "ja", "en" };
    
    /// <summary>
    /// Initializes a new instance of LocalizationService.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentCulture = CultureInfo.CurrentUICulture;
        
        // Initialize resource manager using the full resource name
        try
        {
            // Use the resource name directly: basename is "ServiceWatcher.Resources.Strings"
            _resourceManager = new ResourceManager("ServiceWatcher.Resources.Strings", typeof(LocalizationService).Assembly);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize resource manager");
        }
        
        _logger.LogInformation("LocalizationService initialized. Current culture: {Culture}", _currentCulture.Name);
    }
    
    /// <inheritdoc />
    public string DetectDefaultLanguage()
    {
        var twoLetterCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        var language = twoLetterCode == "ja" ? "ja" : "en";
        
        _logger.LogInformation("Detected default language: {Language} (OS culture: {Culture})", 
            language, CultureInfo.CurrentUICulture.Name);
        
        return language;
    }
    
    /// <inheritdoc />
    public Result<bool> SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return Result<bool>.Failure("Language code cannot be empty");
        }
        
        languageCode = languageCode.ToLowerInvariant();
        
        if (!SupportedLanguages.Contains(languageCode))
        {
            return Result<bool>.Failure($"Unsupported language: {languageCode}. Supported: {string.Join(", ", SupportedLanguages)}");
        }
        
        try
        {
            _currentCulture = new CultureInfo(languageCode);
            
            // Set thread culture
            Thread.CurrentThread.CurrentUICulture = _currentCulture;
            Thread.CurrentThread.CurrentCulture = _currentCulture;
            
            // Set default culture for future threads
            CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;
            CultureInfo.DefaultThreadCurrentCulture = _currentCulture;
            
            _logger.LogInformation("Language changed to: {Language}", languageCode);
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set language to {Language}", languageCode);
            return Result<bool>.Failure($"Failed to set language: {ex.Message}");
        }
    }
    
    /// <inheritdoc />
    public void ApplyResourcesTo(Form form)
    {
        if (form == null)
        {
            throw new ArgumentNullException(nameof(form));
        }
        
        try
        {
            var resources = new ComponentResourceManager(form.GetType());
            
            // Apply resources to the form itself
            resources.ApplyResources(form, "$this", _currentCulture);
            
            // Recursively apply to all controls
            ApplyResourcesToControl(form, resources);
            
            _logger.LogDebug("Applied localized resources to {FormType}", form.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply resources to {FormType}", form.GetType().Name);
        }
    }
    
    /// <summary>
    /// Recursively applies resources to a control and its children.
    /// </summary>
    private void ApplyResourcesToControl(Control control, ComponentResourceManager resources)
    {
        foreach (Control child in control.Controls)
        {
            try
            {
                resources.ApplyResources(child, child.Name, _currentCulture);
                
                // Recursively apply to children
                if (child.HasChildren)
                {
                    ApplyResourcesToControl(child, resources);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not apply resources to control {ControlName}", child.Name);
            }
        }
    }
    
    /// <inheritdoc />
    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }
        
        if (_resourceManager == null)
        {
            _logger.LogWarning("Resource manager not initialized, returning key: {Key}", key);
            return key;
        }
        
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? key;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get string for key: {Key}", key);
            return key;
        }
    }
    
    /// <inheritdoc />
    public string GetFormattedString(string key, params object[] args)
    {
        var format = GetString(key);
        
        if (args == null || args.Length == 0)
        {
            return format;
        }
        
        try
        {
            return string.Format(_currentCulture, format, args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format string for key: {Key}", key);
            return format;
        }
    }
}
