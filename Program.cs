using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using ServiceWatcher.Services;
using ServiceWatcher.UI;
using ServiceWatcher.Utils;

namespace ServiceWatcher;

[ExcludeFromCodeCoverage]
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Initialize logger
        Logger.Initialize();
        
        // Load and apply language settings
        ApplyLanguageSettings();
        
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // Note: ApplicationConfiguration.Initialize() is not available in .NET 8
        // Use Application.SetHighDpiMode() if needed
        Application.Run(new MainForm());
    }

    /// <summary>
    /// Loads language settings from config.json and applies to current thread.
    /// </summary>
    private static void ApplyLanguageSettings()
    {
        try
        {
            var configPath = SimpleConfigLoader.GetDefaultConfigPath();
            if (!File.Exists(configPath))
            {
                // Use default language from OS
                return;
            }

            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("uiLanguage", out var languageProp))
            {
                var languageCode = languageProp.GetString();
                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    var culture = new CultureInfo(languageCode);
                    Thread.CurrentThread.CurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - will use default language
            Console.WriteLine($"Failed to apply language settings: {ex.Message}");
        }
    }
}