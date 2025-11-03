using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceWatcher.Models;
using ServiceWatcher.Services;
using ServiceWatcher.Utils;
using Xunit;

namespace ServiceWatcher.Tests.Unit.Services;

public class ServiceMonitorTests
{
    private static async Task<bool> WaitForAsync(Func<bool> condition, int timeoutMs = 1000, int pollIntervalMs = 25)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition()) return true;
            await Task.Delay(pollIntervalMs);
        }
        return condition();
    }
    private class TestServiceMonitor : ServiceMonitor
    {
        private readonly Queue<ServiceStatus> _statusSequence;
        public List<ServiceStatusChange> Changes { get; } = new();
        public List<MonitoringErrorEventArgs> Errors { get; } = new();

        public TestServiceMonitor(ILogger<ServiceMonitor> logger, IEnumerable<ServiceStatus> statusSequence)
            : base(logger)
        {
            _statusSequence = new Queue<ServiceStatus>(statusSequence);
            ServiceStatusChanged += (_, e) => Changes.Add(e.StatusChange);
            MonitoringError += (_, e) => Errors.Add(e);
        }

    internal override async Task<ServiceStatus> GetCurrentStatusAsync(string serviceName)
        {
            await Task.Yield();
            if (_statusSequence.Count == 0)
            {
                return ServiceStatus.Running; // default
            }
            return _statusSequence.Dequeue();
        }

    internal override async Task CheckAllServicesAsync()
        {
            await base.CheckAllServicesAsync();
        }
    }

    private MonitoredService CreateService(string name) => new()
    {
        ServiceName = name,
        DisplayName = name + "Display",
        NotificationEnabled = true,
        LastKnownStatus = ServiceStatus.Running,
        IsAvailable = true
    };

    [Fact]
    public async Task StartMonitoring_NoServices_Fails()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        var result = await monitor.StartMonitoringAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal("No services configured for monitoring", result.Error);
    }

    [Fact]
    public async Task AddService_Valid_AddsSuccessfully()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        var service = CreateService("svc1");
        var addResult = await monitor.AddServiceAsync(service);
        Assert.True(addResult.IsSuccess);
        Assert.Single(monitor.MonitoredServices);
    }

    [Fact]
    public async Task AddService_Duplicate_Fails()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        var service = CreateService("svc1");
        await monitor.AddServiceAsync(service);
        var second = await monitor.AddServiceAsync(service);
        Assert.False(second.IsSuccess);
        Assert.Equal("Service 'svc1' is already being monitored", second.Error);
    }

    [Fact]
    public async Task StatusChange_EventRaised_OnTransition()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        // Sequence: initial Running (known), then Stopped triggers change
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running, ServiceStatus.Stopped });
        await monitor.AddServiceAsync(CreateService("svc1"));
        var startResult = await monitor.StartMonitoringAsync();
        Assert.True(startResult.IsSuccess);

        // First poll executed by StartMonitoringAsync (initial status refresh)
        // Manually invoke second poll to cause transition
        await monitor.CheckAllServicesAsync();
        // Wait for async event handler to record the change
        var received = await WaitForAsync(() => monitor.Changes.Count == 1, 500);
        Assert.True(received, "ServiceStatusChanged event was not raised within timeout");
        var change = monitor.Changes.Single();
        Assert.Equal(ServiceStatus.Running, change.PreviousStatus);
        Assert.Equal(ServiceStatus.Stopped, change.CurrentStatus);
        Assert.Equal("svc1", change.ServiceName);
    }

    [Fact]
    public async Task RemoveService_RemovesSuccessfully()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        await monitor.AddServiceAsync(CreateService("svc1"));
        var removeResult = await monitor.RemoveServiceAsync("svc1");
        Assert.True(removeResult.IsSuccess);
        Assert.Empty(monitor.MonitoredServices);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public async Task UpdateMonitoringInterval_Invalid_Fails(int interval)
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        var result = await monitor.UpdateMonitoringIntervalAsync(interval);
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3600)]
    public async Task UpdateMonitoringInterval_Valid_Succeeds(int interval)
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        var result = await monitor.UpdateMonitoringIntervalAsync(interval);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RefreshMonitoredServices_WithConfigManager_UpdatesList()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var configManager = new Mock<IConfigurationManager>();
        var newConfig = new ApplicationConfiguration
        {
            MonitoringIntervalSeconds = 5,
            MonitoredServices = new List<MonitoredService>
            {
                CreateService("svc2"),
                CreateService("svc3")
            }
        };
        configManager.Setup(x => x.LoadAsync()).ReturnsAsync(Result<ApplicationConfiguration>.Success(newConfig));
        
        var monitor = new ServiceMonitor(logger.Object, configManager.Object);
        await monitor.AddServiceAsync(CreateService("svc1"));
        
        var result = await monitor.RefreshMonitoredServicesAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(2, monitor.MonitoredServices.Count);
        Assert.Contains(monitor.MonitoredServices, s => s.ServiceName == "svc2");
        Assert.Contains(monitor.MonitoredServices, s => s.ServiceName == "svc3");
    }

    [Fact]
    public async Task StatusChange_NoChange_EventNotRaised()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        // Both polls return same status - no change
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running, ServiceStatus.Running });
        await monitor.AddServiceAsync(CreateService("svc1"));
        await monitor.StartMonitoringAsync();
        await monitor.CheckAllServicesAsync();
        
        // Wait a bit to ensure no event fires
        await Task.Delay(100);
        Assert.Empty(monitor.Changes);
    }

    [Fact]
    public async Task RemoveService_NonExistent_Fails()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        var result = await monitor.RemoveServiceAsync("nonexistent");
        Assert.False(result.IsSuccess);
        Assert.Contains("is not in the", result.Error);
    }

    [Fact]
    public async Task AddService_Null_Fails()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        var result = await monitor.AddServiceAsync(null!);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task StopMonitoring_NotRunning_ReturnsFailure()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        await monitor.AddServiceAsync(CreateService("svc1"));
        
        // Stop without starting should fail
        var result = await monitor.StopMonitoringAsync();
        Assert.False(result.IsSuccess);
        Assert.Contains("not active", result.Error);
    }

    [Fact]
    public async Task GetCurrentStatus_ReturnsLatest()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Stopped });
        await monitor.AddServiceAsync(CreateService("svc1"));
        
        // Trigger status fetch
        await monitor.CheckAllServicesAsync();
        
        var service = monitor.MonitoredServices[0];
        Assert.Equal(ServiceStatus.Stopped, service.LastKnownStatus);
    }

    [Fact]
    public async Task MultipleServices_SimultaneousChanges_AllEventsRaised()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        // Each service transitions: Running -> Stopped
        var monitor = new TestServiceMonitor(logger.Object, new[] { 
            ServiceStatus.Running, ServiceStatus.Running,  // svc1, svc2 initial
            ServiceStatus.Stopped, ServiceStatus.Stopped   // svc1, svc2 changed
        });
        
        await monitor.AddServiceAsync(CreateService("svc1"));
        await monitor.AddServiceAsync(CreateService("svc2"));
        await monitor.StartMonitoringAsync();
        await monitor.CheckAllServicesAsync();
        
        var received = await WaitForAsync(() => monitor.Changes.Count == 2, 500);
        Assert.True(received, "Expected 2 status changes");
        Assert.Contains(monitor.Changes, c => c.ServiceName == "svc1");
        Assert.Contains(monitor.Changes, c => c.ServiceName == "svc2");
    }

    [Fact]
    public async Task IsMonitoring_TrueWhenActive_FalseWhenStopped()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        await monitor.AddServiceAsync(CreateService("svc1"));
        
        Assert.False(monitor.IsMonitoring);
        
        await monitor.StartMonitoringAsync();
        Assert.True(monitor.IsMonitoring);
        
        await monitor.StopMonitoringAsync();
        Assert.False(monitor.IsMonitoring);
    }

    [Fact]
    public async Task StartMonitoring_AlreadyRunning_Fails()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running });
        await monitor.AddServiceAsync(CreateService("svc1"));
        
        await monitor.StartMonitoringAsync();
        var secondStart = await monitor.StartMonitoringAsync();
        
        Assert.False(secondStart.IsSuccess);
        Assert.Contains("already active", secondStart.Error);
    }

    [Fact]
    public async Task StopMonitoring_CancelsPolling()
    {
        var logger = new Mock<ILogger<ServiceMonitor>>();
        var monitor = new TestServiceMonitor(logger.Object, new[] { ServiceStatus.Running, ServiceStatus.Running });
        await monitor.AddServiceAsync(CreateService("svc1"));
        
        await monitor.StartMonitoringAsync();
        var stopResult = await monitor.StopMonitoringAsync();
        
        Assert.True(stopResult.IsSuccess);
        Assert.False(monitor.IsMonitoring);
        
        // After stop, no more status changes should be recorded
        var countBefore = monitor.Changes.Count;
        await Task.Delay(100);
        Assert.Equal(countBefore, monitor.Changes.Count);
    }
}
