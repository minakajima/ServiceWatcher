using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Services;
using ServiceWatcher.Models;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Tests.Unit;

public class ServiceMonitorTests
{
    private readonly Mock<ILogger<ServiceMonitor>> _mockLogger;
    private readonly ServiceMonitor _serviceMonitor;

    public ServiceMonitorTests()
    {
        _mockLogger = new Mock<ILogger<ServiceMonitor>>();
        _serviceMonitor = new ServiceMonitor(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_InitializesWithEmptyServiceList()
    {
        // Assert
        Assert.NotNull(_serviceMonitor);
        Assert.False(_serviceMonitor.IsMonitoring);
        Assert.Empty(_serviceMonitor.MonitoredServices);
    }

    [Fact]
    public async Task AddServiceAsync_AddsValidService_ReturnsSuccess()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "TestService",
            DisplayName = "Test Service",
            NotificationEnabled = true
        };

        // Act
        var result = await _serviceMonitor.AddServiceAsync(service);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(_serviceMonitor.MonitoredServices);
        Assert.Equal("TestService", _serviceMonitor.MonitoredServices[0].ServiceName);
    }

    [Fact]
    public async Task AddServiceAsync_WithNullService_ReturnsFailure()
    {
        // Act
        var result = await _serviceMonitor.AddServiceAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be null", result.Error);
    }

    [Fact]
    public async Task AddServiceAsync_WithInvalidService_ReturnsFailure()
    {
        // Arrange - Service with empty name is invalid
        var service = new MonitoredService
        {
            ServiceName = "",
            DisplayName = "Test Service"
        };

        // Act
        var result = await _serviceMonitor.AddServiceAsync(service);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("invalid", result.Error);
    }

    [Fact]
    public async Task AddServiceAsync_WithDuplicateService_ReturnsFailure()
    {
        // Arrange
        var service1 = new MonitoredService
        {
            ServiceName = "TestService",
            DisplayName = "Test Service 1"
        };
        var service2 = new MonitoredService
        {
            ServiceName = "TestService", // Same name
            DisplayName = "Test Service 2"
        };

        await _serviceMonitor.AddServiceAsync(service1);

        // Act
        var result = await _serviceMonitor.AddServiceAsync(service2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already being monitored", result.Error);
    }

    [Fact]
    public async Task RemoveServiceAsync_RemovesExistingService_ReturnsSuccess()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "TestService",
            DisplayName = "Test Service"
        };
        await _serviceMonitor.AddServiceAsync(service);

        // Act
        var result = await _serviceMonitor.RemoveServiceAsync("TestService");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_serviceMonitor.MonitoredServices);
    }

    [Fact]
    public async Task RemoveServiceAsync_WithNonExistentService_ReturnsFailure()
    {
        // Act
        var result = await _serviceMonitor.RemoveServiceAsync("NonExistent");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not in the monitoring list", result.Error);
    }

    [Fact]
    public async Task StartMonitoringAsync_WithNoServices_ReturnsFailure()
    {
        // Act
        var result = await _serviceMonitor.StartMonitoringAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No services configured", result.Error);
    }

    [Fact]
    public async Task StartMonitoringAsync_WithServices_SetsIsMonitoringTrue()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "Spooler", // Print Spooler - exists on most Windows systems
            DisplayName = "Print Spooler"
        };
        await _serviceMonitor.AddServiceAsync(service);

        // Act
        var result = await _serviceMonitor.StartMonitoringAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(_serviceMonitor.IsMonitoring);

        // Cleanup
        await _serviceMonitor.StopMonitoringAsync();
    }

    [Fact]
    public async Task StopMonitoringAsync_WhenMonitoring_SetsIsMonitoringFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "Spooler",
            DisplayName = "Print Spooler"
        };
        await _serviceMonitor.AddServiceAsync(service);
        await _serviceMonitor.StartMonitoringAsync();

        // Act
        var result = await _serviceMonitor.StopMonitoringAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(_serviceMonitor.IsMonitoring);
    }

    [Fact]
    public async Task StopMonitoringAsync_WhenNotMonitoring_ReturnsFailure()
    {
        // Act
        var result = await _serviceMonitor.StopMonitoringAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not active", result.Error);
    }

    [Fact]
    public async Task UpdateMonitoringIntervalAsync_WithValidInterval_ReturnsSuccess()
    {
        // Act
        var result = await _serviceMonitor.UpdateMonitoringIntervalAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateMonitoringIntervalAsync_WithInvalidInterval_ReturnsFailure()
    {
        // Act - Less than 1 second
        var result1 = await _serviceMonitor.UpdateMonitoringIntervalAsync(0);
        // Act - More than 3600 seconds
        var result2 = await _serviceMonitor.UpdateMonitoringIntervalAsync(3601);

        // Assert
        Assert.False(result1.IsSuccess);
        Assert.False(result2.IsSuccess);
        Assert.Contains("between 1 and 3600", result1.Error);
    }

    [Fact]
    public async Task GetServiceStatusAsync_WithExistingService_ReturnsService()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "TestService",
            DisplayName = "Test Service"
        };
        await _serviceMonitor.AddServiceAsync(service);

        // Act
        var result = await _serviceMonitor.GetServiceStatusAsync("TestService");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TestService", result.Value.ServiceName);
    }

    [Fact]
    public async Task GetServiceStatusAsync_WithNonExistentService_ReturnsFailure()
    {
        // Act
        var result = await _serviceMonitor.GetServiceStatusAsync("NonExistent");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not in the monitoring list", result.Error);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - Should not throw
        _serviceMonitor.Dispose();
        _serviceMonitor.Dispose();
    }
}
