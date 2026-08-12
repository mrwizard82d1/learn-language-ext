namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public class ExpenseSummaryTests
{
    [Fact]
    public void SmokeTest() => LangExtAssert.Equal(4, 2 + 2);

    [Fact]
    public void Map_Find_ReturnsSomeForHit_NoneForMiss()
    {
        var m = Map(("Food", 8m), ("Gas", 10m));
        
        LangExtAssert.Equal(Some(8m), m.Find("Food"));
        LangExtAssert.Equal(Option<decimal>.None, m.Find("Rent"));
    }
}