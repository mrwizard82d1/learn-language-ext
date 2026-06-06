namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;
public class LangExtAssertTests
{
    [Fact]
    public void Equal_OnMismatch_FailsWithReadableString()
    {
        var expected = Some(1);
        var actual = None;

        var ex = Record.Exception(() => LangExtAssert.Equal(expected, actual));

        Assert.NotNull(ex);  // It failed, ... 
        Assert.Contains("Some(1)", ex.Message); // ...it printed the readable forms, ...
        Assert.Contains("None", ex.Message); // ...and it is **not** a `[[...]]` collection dump
    }

    [Fact]
    public void Equal_OnMatch_DoesNotThrow()
    {
        LangExtAssert.Equal(Some(1), Some(1)); // **No throw** => pass
    }
}