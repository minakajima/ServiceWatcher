using System.Diagnostics.CodeAnalysis;
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
        
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // Note: ApplicationConfiguration.Initialize() is not available in .NET 8
        // Use Application.SetHighDpiMode() if needed
        Application.Run(new MainForm());
    }    
}