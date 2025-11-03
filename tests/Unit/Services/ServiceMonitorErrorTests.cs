using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceWatcher.Models;
using ServiceWatcher.Services;
using Xunit;

namespace ServiceWatcher.Tests.Unit.Services;

public class ServiceMonitorErrorTests
{
    private class ErrorServiceMonitor : ServiceMonitor
    {
        private readonly Queue<Func<ServiceStatus>> _behaviors;
        public List<MonitoringErrorEventArgs> Errors { get; } = new();

        public ErrorServiceMonitor(ILogger<ServiceMonitor> logger, IEnumerable<Func<ServiceStatus>> behaviors)
            : base(logger)
        {
            _behaviors = new Queue<Func<ServiceStatus>>(behaviors);
            MonitoringError += (_, e) => Errors.Add(e);
        }

    internal override async Task<ServiceStatus> GetCurrentStatusAsync(string serviceName)
        {
            await Task.Yield();
            var next = _behaviors.Count > 0 ? _behaviors.Dequeue() : (() => ServiceStatus.Running);
            return next();
        }

    internal override async Task CheckAllServicesAsync()
        {
            await base.CheckAllServicesAsync();
        }
    }

    private MonitoredService Svc(string name) => new()
    {
        ServiceName = name,
        DisplayName = name + "Display",
        NotificationEnabled = true,
        LastKnownStatus = ServiceStatus.Unknown,
        IsAvailable = true
    };

    [Fact]
    public async Task MonitoringError_InvalidOperation_Raised()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new ErrorServiceMonitor(logger.Object, new Func<ServiceStatus>[]
        {
            () => throw new InvalidOperationException("Service not found")
        });
        await monitor.AddServiceAsync(Svc("svc_err"));
        var start = await monitor.StartMonitoringAsync();
        Assert.True(start.IsSuccess);
        await monitor.CheckAllServicesAsync();
        await Task.Delay(50); // Wait for error event to be raised
        Assert.Single(monitor.Errors);
        Assert.Contains("Service not found", monitor.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task MonitoringError_Win32_Raised()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new ErrorServiceMonitor(logger.Object, new Func<ServiceStatus>[]
        {
            () => throw new Win32Exception(5) // Access denied
        });
        await monitor.AddServiceAsync(Svc("svc_win32"));
        var start = await monitor.StartMonitoringAsync();
        Assert.True(start.IsSuccess);
        await monitor.CheckAllServicesAsync();
        await Task.Delay(50); // Wait for error event to be raised
        Assert.Single(monitor.Errors);
        Assert.Contains("Access denied", monitor.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task MonitoringError_Generic_Raised()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new ErrorServiceMonitor(logger.Object, new Func<ServiceStatus>[]
        {
            () => throw new Exception("Unexpected boom")
        });
        await monitor.AddServiceAsync(Svc("svc_generic"));
        var start = await monitor.StartMonitoringAsync();
        Assert.True(start.IsSuccess);
        await monitor.CheckAllServicesAsync();
        await Task.Delay(50); // Wait for error event to be raised
        Assert.Single(monitor.Errors);
        Assert.Contains("Unexpected error", monitor.Errors[0].ErrorMessage);
    }
}
