using ServiceWatcher.Utils;

namespace ServiceWatcher.Tests.Unit.Utils;

public class ResultTests
{
    [Fact]
    public void Success_WithValue_CreatesSuccessResult()
    {
        var value = "test value";
        var result = Result<string>.Success(value);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(value, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_WithError_CreatesFailureResult()
    {
        var error = "Error message";
        var result = Result<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_WithIntValue_CreatesSuccessResult()
    {
        var value = 42;
        var result = Result<int>.Success(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Failure_WithIntType_CreatesFailureResult()
    {
        var error = "Integer error";
        var result = Result<int>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_NullValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result<string>.Success(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failure_InvalidError_ThrowsArgumentException(string error)
    {
        Assert.Throws<ArgumentException>(() => Result<string>.Failure(error));
    }

    [Fact]
    public void Value_OnFailureResult_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure("Error");

        var ex = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("Cannot access Value", ex.Message);
    }

    [Fact]
    public void Error_OnSuccessResult_ReturnsNull()
    {
        var result = Result<string>.Success("value");

        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_WithComplexType_WorksCorrectly()
    {
        var obj = new { Name = "Test", Value = 123 };
        var result = Result<object>.Success(obj);

        Assert.True(result.IsSuccess);
        Assert.Equal(obj, result.Value);
    }
}
