using System.Diagnostics.CodeAnalysis;
using ServiceWatcher.Models;

namespace ServiceWatcher.Utils;

/// <summary>
/// Validates configuration settings.
/// Static utility methods used directly by UI forms for immediate validation.
/// </summary>
[ExcludeFromCodeCoverage] // UI直接使用のため単体テスト対象外
public static class ConfigurationValidator
{
    /// <summary>
    /// Validates monitoring interval.
    /// </summary>
    public static ValidationResult ValidateMonitoringInterval(int intervalSeconds)
    {
        var errors = new List<string>();

        if (intervalSeconds < 1)
        {
            errors.Add("監視間隔は1秒以上である必要があります。");
        }

        if (intervalSeconds > 3600)
        {
            errors.Add("監視間隔は3600秒（1時間）以下である必要があります。");
        }

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validates notification display time.
    /// </summary>
    public static ValidationResult ValidateNotificationDisplayTime(int displayTimeSeconds)
    {
        var errors = new List<string>();

        if (displayTimeSeconds < 0)
        {
            errors.Add("通知表示時間は0秒以上である必要があります。");
        }

        if (displayTimeSeconds > 300)
        {
            errors.Add("通知表示時間は300秒（5分）以下である必要があります。");
        }

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validates monitored services list.
    /// </summary>
    public static ValidationResult ValidateMonitoredServices(List<MonitoredService> services)
    {
        var errors = new List<string>();

        // Check max count
        if (services.Count > 50)
        {
            errors.Add("監視対象サービスは最大50個までです。");
        }

        // Check for duplicates
        var duplicates = services
            .GroupBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            errors.Add($"重複するサービス名があります: {string.Join(", ", duplicates)}");
        }

        // Check service name length
        var tooLongNames = services
            .Where(s => s.ServiceName.Length > 256)
            .Select(s => s.ServiceName)
            .ToList();

        if (tooLongNames.Any())
        {
            errors.Add($"サービス名が256文字を超えています: {string.Join(", ", tooLongNames.Select(n => n.Substring(0, 50) + "..."))}");
        }

        // Check for empty service names
        var emptyNames = services.Where(s => string.IsNullOrWhiteSpace(s.ServiceName)).ToList();
        if (emptyNames.Any())
        {
            errors.Add("空のサービス名があります。");
        }

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validates entire configuration.
    /// </summary>
    public static ValidationResult ValidateAll(int monitoringInterval, int notificationDisplayTime, List<MonitoredService> services)
    {
        var allErrors = new List<string>();

        var intervalResult = ValidateMonitoringInterval(monitoringInterval);
        if (!intervalResult.IsValid)
        {
            allErrors.AddRange(intervalResult.Errors);
        }

        var displayTimeResult = ValidateNotificationDisplayTime(notificationDisplayTime);
        if (!displayTimeResult.IsValid)
        {
            allErrors.AddRange(displayTimeResult.Errors);
        }

        var servicesResult = ValidateMonitoredServices(services);
        if (!servicesResult.IsValid)
        {
            allErrors.AddRange(servicesResult.Errors);
        }

        return new ValidationResult(allErrors);
    }
}
