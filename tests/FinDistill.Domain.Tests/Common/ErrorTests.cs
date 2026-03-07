using FinDistill.Domain.Common;

namespace FinDistill.Domain.Tests.Common;

public class ErrorTests
{
    [Fact]
    public void Error_None_HasEmptyCodeAndMessage()
    {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Message);
    }

    [Fact]
    public void Error_RecordEquality_SameValues_AreEqual()
    {
        var a = new Error("Code", "Message");
        var b = new Error("Code", "Message");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Error_RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new Error("Code.A", "Message A");
        var b = new Error("Code.B", "Message B");

        Assert.NotEqual(a, b);
    }
}
