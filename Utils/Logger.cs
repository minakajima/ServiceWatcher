using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace ServiceWatcher.Utils;

/// <summary>
/// Simple file-based logger with rotation support.
/// Used in production runtime only, not tested in unit tests.
/// </summary>
[ExcludeFromCodeCoverage] // 実運用時のみ使用、単体テスト対象外
public class Logger : ILogger
{
    private static readonly object _lock = new object();
    private readonly string _categoryName;
    private static string? _logDirectory;
    private static readonly long MaxLogFileSize = 10 * 1024 * 1024; // 10MB
    private static readonly int MaxLogFiles = 10;

    /// <summary>
    /// Initializes the logger with the specified log directory.
    /// </summary>
    /// <param name="logDirectory">Directory path for log files. Defaults to %LOCALAPPDATA%\ServiceWatcher\logs\</param>
    public static void Initialize(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ServiceWatcher",
            "logs");

        Directory.CreateDirectory(_logDirectory);
    }

    public Logger(string categoryName)
    {
        _categoryName = categoryName;
        if (_logDirectory == null)
        {
            Initialize();
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] [{_categoryName}] {message}";

        if (exception != null)
        {
            logEntry += $"{Environment.NewLine}Exception: {exception}";
        }

        WriteLog(logEntry);
    }

    private static void WriteLog(string message)
    {
        lock (_lock)
        {
            try
            {
                var logFilePath = GetCurrentLogFilePath();
                RotateLogFileIfNeeded(logFilePath);
                File.AppendAllText(logFilePath, message + Environment.NewLine);
            }
            catch
            {
                // Fail silently to prevent logging from crashing the application
            }
        }
    }

    private static string GetCurrentLogFilePath()
    {
        var fileName = $"ServiceWatcher_{DateTime.Now:yyyyMMdd}.log";
        return Path.Combine(_logDirectory!, fileName);
    }

    private static void RotateLogFileIfNeeded(string logFilePath)
    {
        if (!File.Exists(logFilePath))
            return;

        var fileInfo = new FileInfo(logFilePath);
        if (fileInfo.Length < MaxLogFileSize)
            return;

        // Rotate: rename current log file with timestamp
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var rotatedFileName = Path.GetFileNameWithoutExtension(logFilePath) + $"_{timestamp}.log";
        var rotatedFilePath = Path.Combine(_logDirectory!, rotatedFileName);
        File.Move(logFilePath, rotatedFilePath);

        // Clean up old log files (keep only MaxLogFiles)
        var logFiles = Directory.GetFiles(_logDirectory!, "*.log")
            .OrderByDescending(f => File.GetCreationTime(f))
            .Skip(MaxLogFiles)
            .ToList();

        foreach (var oldFile in logFiles)
        {
            try
            {
                File.Delete(oldFile);
            }
            catch
            {
                // Ignore deletion failures
            }
        }
    }
}

/// <summary>
/// Logger factory for creating Logger instances.
/// Used in production runtime only, not tested in unit tests.
/// </summary>
[ExcludeFromCodeCoverage] // 実運用時のみ使用、単体テスト対象外
public class LoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
        // Not implemented - simple logger doesn't use providers
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new Logger(categoryName);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
