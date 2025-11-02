using System.Collections.Generic;
using ServiceWatcher.Models;
using ServiceWatcher.Services;
using Xunit;

namespace ServiceWatcher.Tests.Unit.Services;

public class ConfigurationValidatorTests
{
    [Fact]
    public void DuplicateServiceNames_FailsValidation()
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>
            {
                new() { ServiceName = "svc1", DisplayName = "Service 1", NotificationEnabled = true, IsAvailable = true },
                new() { ServiceName = "SVC1", DisplayName = "Service 1 Duplicate", NotificationEnabled = true, IsAvailable = true }
            }
        };
        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("重複したサービス名"));
    }

    [Fact]
    public void ExcessiveServiceCount_FailsValidation()
    {
        var services = new List<MonitoredService>();
        for (int i = 0; i < 101; i++)
        {
            services.Add(new MonitoredService { ServiceName = "svc" + i, DisplayName = "Service" + i, NotificationEnabled = true, IsAvailable = true });
        }
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = services
        };
        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("監視サービス数は100個以下"));
    }
}
