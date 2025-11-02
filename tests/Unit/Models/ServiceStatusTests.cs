using ServiceWatcher.Models;

namespace ServiceWatcher.Tests.Unit.Models;

public class ServiceStatusTests
{
    [Fact]
    public void ServiceStatus_HasExpectedValues()
    {
        Assert.Equal(0, (int)ServiceStatus.Unknown);
        Assert.Equal(1, (int)ServiceStatus.Stopped);
        Assert.Equal(2, (int)ServiceStatus.StartPending);
        Assert.Equal(3, (int)ServiceStatus.StopPending);
        Assert.Equal(4, (int)ServiceStatus.Running);
        Assert.Equal(5, (int)ServiceStatus.ContinuePending);
        Assert.Equal(6, (int)ServiceStatus.PausePending);
        Assert.Equal(7, (int)ServiceStatus.Paused);
    }

    [Theory]
    [InlineData(ServiceStatus.Unknown, "Unknown")]
    [InlineData(ServiceStatus.Stopped, "Stopped")]
    [InlineData(ServiceStatus.Running, "Running")]
    [InlineData(ServiceStatus.Paused, "Paused")]
    public void ServiceStatus_ToString_ReturnsExpectedName(ServiceStatus status, string expected)
    {
        Assert.Equal(expected, status.ToString());
    }
}
