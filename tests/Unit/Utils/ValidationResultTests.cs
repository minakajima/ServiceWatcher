using Xunit;
using ServiceWatcher.Utils;

namespace ServiceWatcher.Tests.Unit.Utils;

public class ValidationResultTests
{
    [Fact]
    public void Success_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithSingleError_CreatesInvalidResult()
    {
        // Act
        var result = ValidationResult.Failure("Error message");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Error message", result.Errors[0]);
    }

    [Fact]
    public void Failure_WithMultipleErrors_CreatesInvalidResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
    }

    [Fact]
    public void AddError_AddsErrorToList()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Error message");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void AddError_WithEmptyString_DoesNotAddError()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void AddError_WithWhitespace_DoesNotAddError()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("   ");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Failure_WithNullOrEmptyError_CreatesResultWithEmptyList(string? errorMessage)
    {
        // Act
        var result = ValidationResult.Failure(new[] { errorMessage! });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetCombinedErrorMessage_WithSingleError_ReturnsMessage()
    {
        // Arrange
        var result = ValidationResult.Failure("Error message");

        // Act
        var message = result.GetCombinedErrorMessage();

        // Assert
        Assert.Equal("Error message", message);
    }

    [Fact]
    public void GetCombinedErrorMessage_WithMultipleErrors_ReturnsJoinedMessage()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = ValidationResult.Failure(errors);

        // Act
        var message = result.GetCombinedErrorMessage();

        // Assert
        Assert.Contains("Error 1", message);
        Assert.Contains("Error 2", message);
    }

    [Fact]
    public void GetCombinedErrorMessage_WithNoErrors_ReturnsEmptyString()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act
        var message = result.GetCombinedErrorMessage();

        // Assert
        Assert.Equal(string.Empty, message);
    }

    [Fact]
    public void Constructor_WithErrors_InitializesErrorList()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = new ValidationResult(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }
}
