using Xunit;
using ServiceWatcher.Models;

namespace ServiceWatcher.Tests.Unit.Models;

public class ServiceStatusChangeTests
{
    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Act
        var change = new ServiceStatusChange();

        // Assert
        Assert.Equal(string.Empty, change.ServiceName);
        Assert.Equal(string.Empty, change.DisplayName);
        Assert.Equal(ServiceStatus.Unknown, change.PreviousStatus);
        Assert.Equal(ServiceStatus.Unknown, change.CurrentStatus);
        Assert.False(change.NotificationShown);
        Assert.False(change.UserAcknowledged);
    }

    [Fact]
    public void IsStopEvent_RunningToStopped_ReturnsTrue()
    {
        // Arrange
        var change = new ServiceStatusChange
        {
            PreviousStatus = ServiceStatus.Running,
            CurrentStatus = ServiceStatus.Stopped
        };

        // Act & Assert
        Assert.True(change.IsStopEvent);
    }

    [Theory]
    [InlineData(ServiceStatus.Running, ServiceStatus.Stopped, true)]
    [InlineData(ServiceStatus.Running, ServiceStatus.StopPending, true)]
    [InlineData(ServiceStatus.Paused, ServiceStatus.Stopped, true)]
    [InlineData(ServiceStatus.StartPending, ServiceStatus.Stopped, true)]
    [InlineData(ServiceStatus.ContinuePending, ServiceStatus.Stopped, true)]
    [InlineData(ServiceStatus.Stopped, ServiceStatus.Running, false)]
    [InlineData(ServiceStatus.StartPending, ServiceStatus.Running, false)]
    [InlineData(ServiceStatus.ContinuePending, ServiceStatus.Running, false)]
    [InlineData(ServiceStatus.Running, ServiceStatus.Paused, false)]
    [InlineData(ServiceStatus.Running, ServiceStatus.PausePending, false)]
    public void IsStopEvent_VariousTransitions_ReturnsExpectedValue(
        ServiceStatus previous, ServiceStatus current, bool expected)
    {
        // Arrange
        var change = new ServiceStatusChange
        {
            PreviousStatus = previous,
            CurrentStatus = current
        };

        // Act & Assert
        Assert.Equal(expected, change.IsStopEvent);
    }

    [Fact]
    public void PropertySetters_WorkCorrectly()
    {
        // Arrange
        var change = new ServiceStatusChange();

        // Act
        change.ServiceName = "TestService";
        change.DisplayName = "Test Service Display";
        change.PreviousStatus = ServiceStatus.Stopped;
        change.CurrentStatus = ServiceStatus.Running;
        change.DetectedAt = DateTime.Now;
        change.NotificationShown = true;
        change.UserAcknowledged = true;

        // Assert
        Assert.Equal("TestService", change.ServiceName);
        Assert.Equal("Test Service Display", change.DisplayName);
        Assert.Equal(ServiceStatus.Stopped, change.PreviousStatus);
        Assert.Equal(ServiceStatus.Running, change.CurrentStatus);
        Assert.True(change.NotificationShown);
        Assert.True(change.UserAcknowledged);
    }
}
