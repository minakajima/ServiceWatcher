using System;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceWatcher.Models;
using ServiceWatcher.Services;
using Xunit;

namespace ServiceWatcher.Tests.Unit.Services;

public class NotificationServiceTests
{
    private ServiceStatusChange Change(string name, ServiceStatus prev, ServiceStatus curr) => new()
    {
        ServiceName = name,
        DisplayName = name + "Display",
        PreviousStatus = prev,
        CurrentStatus = curr,
        DetectedAt = DateTime.Now
    };

    [Fact]
    public void ShowNotification_InvalidDisplayTime_Fails()
    {
        var logger = new Mock<ILogger<NotificationService>>();
    var svc = new NotificationService(logger.Object, simulate: true);
        var change = Change("svc1", ServiceStatus.Running, ServiceStatus.Stopped);
        var result = svc.ShowNotification(change, -1);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ShowNotification_ZeroDisplayTime_NoTimer()
    {
        var logger = new Mock<ILogger<NotificationService>>();
    var svc = new NotificationService(logger.Object, simulate: true);
        var change = Change("svc1", ServiceStatus.Running, ServiceStatus.Stopped);
        var result = svc.ShowNotification(change, 0);
        Assert.True(result.IsSuccess);
        Assert.True(change.NotificationShown);
        Assert.Equal(1, svc.ActiveNotificationCount);
        // Manually close
        svc.CloseNotification("svc1");
        Assert.Equal(0, svc.ActiveNotificationCount);
    }

    [Fact]
    public void ShowNotification_Duplicate_Ignored()
    {
        var logger = new Mock<ILogger<NotificationService>>();
    var svc = new NotificationService(logger.Object, simulate: true);
        var change = Change("svc1", ServiceStatus.Running, ServiceStatus.Stopped);
        var r1 = svc.ShowNotification(change, 0);
        var r2 = svc.ShowNotification(change, 0);
        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.Equal(1, svc.ActiveNotificationCount);
    }

    [Fact]
    public void CloseAllNotifications_EmptiesActive()
    {
        var logger = new Mock<ILogger<NotificationService>>();
    var svc = new NotificationService(logger.Object, simulate: true);
        var change1 = Change("svc1", ServiceStatus.Running, ServiceStatus.Stopped);
        var change2 = Change("svc2", ServiceStatus.Running, ServiceStatus.Stopped);
        svc.ShowNotification(change1, 0);
        svc.ShowNotification(change2, 0);
        Assert.Equal(2, svc.ActiveNotificationCount);
        svc.CloseAllNotifications();
        Assert.Equal(0, svc.ActiveNotificationCount);
    }

    [Fact]
    public void NotificationAcknowledged_Event_Raised_OnManualClose()
    {
        var logger = new Mock<ILogger<NotificationService>>();
    var svc = new NotificationService(logger.Object, simulate: true);
        var change = Change("svcAck", ServiceStatus.Running, ServiceStatus.Stopped);
        NotificationAcknowledgedEventArgs? received = null;
        svc.NotificationAcknowledged += (_, e) => received = e;

        svc.ShowNotification(change, 0);
        Assert.Equal(1, svc.ActiveNotificationCount);

        var closed = svc.CloseNotification("svcAck");
        Assert.True(closed);
        Assert.NotNull(received);
        Assert.Equal("svcAck", received!.ServiceName);
        Assert.False(received.WasAutoClosed);
    }

    [Fact]
    public void ShowNotification_NullStatusChange_Fails()
    {
        var logger = new Mock<ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, simulate: true);
        var result = svc.ShowNotification(null!, 10);
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be null", result.Error);
    }

    [Fact]
    public void ShowNotification_ExcessiveDisplayTime_Fails()
    {
        var logger = new Mock<ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, simulate: true);
        var change = Change("svc1", ServiceStatus.Running, ServiceStatus.Stopped);
        var result = svc.ShowNotification(change, 301);
        Assert.False(result.IsSuccess);
        Assert.Contains("between 0 and 300", result.Error);
    }

    [Fact]
    public void CloseNotification_EmptyServiceName_ReturnsFalse()
    {
        var logger = new Mock<ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, simulate: true);
        Assert.False(svc.CloseNotification(""));
        Assert.False(svc.CloseNotification("  "));
    }

    [Fact]
    public void CloseNotification_NonExistent_ReturnsFalse()
    {
        var logger = new Mock<ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, simulate: true);
        Assert.False(svc.CloseNotification("nonexistent"));
    }

    [Fact]
    public void GetActiveNotifications_ReturnsReadOnlyList()
    {
        var logger = new Mock<ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, simulate: true);
        var change1 = Change("svc1", ServiceStatus.Running, ServiceStatus.Stopped);
        var change2 = Change("svc2", ServiceStatus.Running, ServiceStatus.Stopped);
        
        svc.ShowNotification(change1, 0);
        svc.ShowNotification(change2, 0);
        
        var activeList = svc.GetActiveNotifications();
        Assert.Equal(2, activeList.Count);
        Assert.Contains(activeList, n => n.ServiceName == "svc1");
        Assert.Contains(activeList, n => n.ServiceName == "svc2");
    }

    [Fact]
    public async Task ShowNotificationAsync_ReturnsSuccess()
    {
        var logger = new Mock<ILogger<NotificationService>>();
        var svc = new NotificationService(logger.Object, simulate: true);
        var change = Change("svcAsync", ServiceStatus.Running, ServiceStatus.Stopped);
        
        var result = await svc.ShowNotificationAsync(change, 0);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(1, svc.ActiveNotificationCount);
    }
}
