using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Services;

/// <summary>
/// Validates ApplicationConfiguration instances according to business rules.
/// </summary>
public class ConfigurationValidator
{
    private readonly ILocalizationService? _localizationService;

    public ConfigurationValidator(ILocalizationService? localizationService = null)
    {
        _localizationService = localizationService;
    }
    /// <summary>
    /// Validates an ApplicationConfiguration instance.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>ValidationResult with any errors found.</returns>
    public ValidationResult Validate(ApplicationConfiguration configuration)
    {
        var result = new ValidationResult();
        
        if (configuration == null)
        {
            result.AddError(_localizationService?.GetString("Validation_ConfigNull") ?? "設定がnullです");
            return result;
        }
        
        // Validate monitoring interval
        if (configuration.MonitoringIntervalSeconds < 1)
        {
            result.AddError(_localizationService?.GetString("Validation_IntervalTooShort") ?? "監視間隔は1秒以上である必要があります");
        }
        else if (configuration.MonitoringIntervalSeconds > 3600)
        {
            result.AddError(_localizationService?.GetString("Validation_IntervalTooLong") ?? "監視間隔は3600秒(1時間)以下である必要があります");
        }
        
        // Validate notification display time
        if (configuration.NotificationDisplayTimeSeconds < 0)
        {
            result.AddError(_localizationService?.GetString("Validation_NotificationTimeNegative") ?? "通知表示時間は0秒以上である必要があります(0 = 手動で閉じる)");
        }
        else if (configuration.NotificationDisplayTimeSeconds > 300)
        {
            result.AddError(_localizationService?.GetString("Validation_NotificationTimeTooLong") ?? "通知表示時間は300秒(5分)以下である必要があります");
        }
        
        // Validate monitored services list
        if (configuration.MonitoredServices == null)
        {
            result.AddError("MonitoredServicesがnullです");
        }
        else
        {
            // Validate individual services
            for (int i = 0; i < configuration.MonitoredServices.Count; i++)
            {
                var service = configuration.MonitoredServices[i];
                
                if (string.IsNullOrWhiteSpace(service.ServiceName))
                {
                    result.AddError(_localizationService?.GetFormattedString("Validation_ServiceNameEmpty", i) ?? $"サービス[{i}]: ServiceNameが空です");
                }
                
                if (string.IsNullOrWhiteSpace(service.DisplayName))
                {
                    result.AddError(_localizationService?.GetFormattedString("Validation_DisplayNameEmpty", i) ?? $"サービス[{i}]: DisplayNameが空です");
                }
            }
            
            // Check for duplicate service names
            var duplicates = configuration.MonitoredServices
                .GroupBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            
            foreach (var duplicate in duplicates)
            {
                result.AddError(_localizationService?.GetFormattedString("Validation_DuplicateService", duplicate) ?? $"重複したサービス名: {duplicate}");
            }
            
            // Validate service count
            if (configuration.MonitoredServices.Count > 100)
            {
                result.AddError(_localizationService?.GetString("Validation_TooManyServices") ?? "監視サービス数は100個以下である必要があります");
            }
        }
        
        return result;
    }
}
