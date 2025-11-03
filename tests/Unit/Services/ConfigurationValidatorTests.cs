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

    [Fact]
    public void Validate_NullConfiguration_ReturnsError()
    {
        var validator = new ConfigurationValidator();
        var result = validator.Validate(null!);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_MonitoringIntervalTooLow_ReturnsError(int interval)
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = interval,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>()
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("1秒以上"));
    }

    [Theory]
    [InlineData(3601)]
    [InlineData(5000)]
    public void Validate_MonitoringIntervalTooHigh_ReturnsError(int interval)
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = interval,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>()
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("3600秒"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(3600)]
    public void Validate_MonitoringIntervalValid_Succeeds(int interval)
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = interval,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>()
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NotificationDisplayTimeNegative_ReturnsError()
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = -1,
            MonitoredServices = new List<MonitoredService>()
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("0秒以上"));
    }

    [Theory]
    [InlineData(301)]
    [InlineData(500)]
    public void Validate_NotificationDisplayTimeTooHigh_ReturnsError(int displayTime)
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = displayTime,
            MonitoredServices = new List<MonitoredService>()
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("300秒"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(300)]
    public void Validate_NotificationDisplayTimeValid_Succeeds(int displayTime)
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = displayTime,
            MonitoredServices = new List<MonitoredService>()
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MonitoredServicesNull_ReturnsError()
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = null!
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null"));
    }

    [Fact]
    public void Validate_EmptyServiceName_ReturnsError()
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>
            {
                new() { ServiceName = "", DisplayName = "Valid Display", NotificationEnabled = true, IsAvailable = true }
            }
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ServiceName") && e.Contains("空"));
    }

    [Fact]
    public void Validate_EmptyDisplayName_ReturnsError()
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>
            {
                new() { ServiceName = "ValidService", DisplayName = "  ", NotificationEnabled = true, IsAvailable = true }
            }
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("DisplayName") && e.Contains("空"));
    }

    [Fact]
    public void Validate_ValidConfiguration_Succeeds()
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            NotificationDisplayTimeSeconds = 30,
            MonitoredServices = new List<MonitoredService>
            {
                new() { ServiceName = "Service1", DisplayName = "Service 1", NotificationEnabled = true, IsAvailable = true },
                new() { ServiceName = "Service2", DisplayName = "Service 2", NotificationEnabled = true, IsAvailable = true }
            }
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var config = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 0,
            NotificationDisplayTimeSeconds = -5,
            MonitoredServices = new List<MonitoredService>
            {
                new() { ServiceName = "", DisplayName = "NoName", NotificationEnabled = true, IsAvailable = true },
                new() { ServiceName = "Valid", DisplayName = "", NotificationEnabled = true, IsAvailable = true }
            }
        };

        var validator = new ConfigurationValidator();
        var result = validator.Validate(config);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
    }
}
