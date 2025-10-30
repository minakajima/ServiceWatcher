using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ServiceWatcher.Services;
using ServiceWatcher.Models;

namespace ServiceWatcher.Tests.Unit;

public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<NotificationService>>();
        _notificationService = new NotificationService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_InitializesWithZeroActiveNotifications()
    {
        // Assert
        Assert.NotNull(_notificationService);
        Assert.Equal(0, _notificationService.ActiveNotificationCount);
    }

    [Fact]
    public void ShowNotification_WithNullStatusChange_ReturnsFailure()
    {
        // Act
        var result = _notificationService.ShowNotification(null!, 30);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be null", result.Error);
    }

    [Fact]
    public void ShowNotification_WithInvalidDisplayTime_ReturnsFailure()
    {
        // Arrange
        var statusChange = new ServiceStatusChange
        {
            ServiceName = "TestService",
            DisplayName = "Test Service",
            PreviousStatus = ServiceStatus.Running,
            CurrentStatus = ServiceStatus.Stopped,
            DetectedAt = DateTime.Now
        };

        // Act - Negative time
        var result1 = _notificationService.ShowNotification(statusChange, -1);
        // Act - Too long (over 300 seconds)
        var result2 = _notificationService.ShowNotification(statusChange, 301);

        // Assert
        Assert.False(result1.IsSuccess);
        Assert.False(result2.IsSuccess);
        Assert.Contains("between 0 and 300", result1.Error);
    }

    [Fact]
    public void ShowNotification_WithValidStatusChange_ReturnsSuccess()
    {
        // Arrange
        var statusChange = new ServiceStatusChange
        {
            ServiceName = "TestService",
            DisplayName = "Test Service",
            PreviousStatus = ServiceStatus.Running,
            CurrentStatus = ServiceStatus.Stopped,
            DetectedAt = DateTime.Now
        };

        // Act
        var result = _notificationService.ShowNotification(statusChange, 30);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Cleanup
        _notificationService.CloseAllNotifications();
    }

    [Fact]
    public async Task ShowNotificationAsync_WithValidStatusChange_ReturnsSuccess()
    {
        // Arrange
        var statusChange = new ServiceStatusChange
        {
            ServiceName = "TestService",
            DisplayName = "Test Service",
            PreviousStatus = ServiceStatus.Running,
            CurrentStatus = ServiceStatus.Stopped,
            DetectedAt = DateTime.Now
        };

        // Act
        var result = await _notificationService.ShowNotificationAsync(statusChange, 30);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Cleanup
        _notificationService.CloseAllNotifications();
    }

    [Fact]
    public void CloseNotification_WithNonExistentService_ReturnsFalse()
    {
        // Act
        var result = _notificationService.CloseNotification("NonExistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CloseAllNotifications_CanBeCalledWhenNoNotifications()
    {
        // Act & Assert - Should not throw
        _notificationService.CloseAllNotifications();
    }

    [Fact]
    public void GetActiveNotifications_InitiallyReturnsEmptyList()
    {
        // Act
        var notifications = _notificationService.GetActiveNotifications();

        // Assert
        Assert.NotNull(notifications);
        Assert.Empty(notifications);
    }
}
