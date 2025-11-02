using Xunit;
using ServiceWatcher.Models;

namespace ServiceWatcher.Tests.Unit.Models;

public class MonitoredServiceTests
{
    [Fact]
    public void IsValid_WithValidService_ReturnsTrue()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "wuauserv",
            DisplayName = "Windows Update",
            LastKnownStatus = ServiceStatus.Running
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithNullServiceName_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = null!,
            DisplayName = "Windows Update"
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithEmptyServiceName_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "",
            DisplayName = "Windows Update"
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithWhitespaceServiceName_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "   ",
            DisplayName = "Windows Update"
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithEmptyDisplayName_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "wuauserv",
            DisplayName = ""
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithServiceNameTooLong_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = new string('a', 257),
            DisplayName = "Windows Update"
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithServiceNameExactly256Chars_ReturnsTrue()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = new string('a', 256),
            DisplayName = "Windows Update"
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithDisplayNameTooLong_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "wuauserv",
            DisplayName = new string('a', 257)
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithFutureLastCheckedDate_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "wuauserv",
            DisplayName = "Windows Update",
            LastChecked = DateTime.Now.AddDays(1)
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithUnavailableServiceAndNoErrorMessage_ReturnsFalse()
    {
        // Arrange
        var service = new MonitoredService
        {
            ServiceName = "wuauserv",
            DisplayName = "Windows Update",
            IsAvailable = false,
            ErrorMessage = null
        };

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }
}
