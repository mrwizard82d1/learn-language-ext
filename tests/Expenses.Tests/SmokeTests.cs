using LanguageExt;

namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public class SmokeTests
{
    [Fact]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void LanguageExtIsAvailable()
    {
        var some = Some(42);
        Assert.True(some.IsSome);
    }
}
