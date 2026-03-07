using FinDistill.Domain.Common;

namespace FinDistill.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccessTrue_IsFailureFalse()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_IsSuccessFalse_IsFailureTrue()
    {
        var error = new Error("Test.Error", "Something went wrong");
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Test.Error", result.Error.Code);
        Assert.Equal("Something went wrong", result.Error.Message);
    }

    [Fact]
    public void SuccessT_ContainsValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void FailureT_AccessingValue_ThrowsInvalidOperationException()
    {
        var error = new Error("Value.Missing", "No value");
        var result = Result.Failure<int>(error);

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void SuccessT_WithReferenceType_ContainsValue()
    {
        var list = new List<string> { "a", "b" };
        var result = Result.Success<IReadOnlyList<string>>(list);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public void FailureT_ErrorPreserved()
    {
        var error = new Error("Extract.Failed", "Provider timeout");
        var result = Result.Failure<string>(error);

        Assert.Equal("Extract.Failed", result.Error.Code);
        Assert.Equal("Provider timeout", result.Error.Message);
    }
}
