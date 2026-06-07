namespace Expenses.Tests;

using System.Globalization;
using LanguageExt;
using static LanguageExt.Prelude;

public static class ExpenseParser
{
    // Correct behavior.
    public static Either<string, decimal> ParseAmount(string s) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) 
            ? Right(d) 
            : Left($"Bad amount: '{s}'");
}
public class ExpenseParserTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);
    
    [Fact]
    public void ParseAmount_ValidNumber_ReturnsRight()
    {
        var result = ExpenseParser.ParseAmount("12.50");
        
        LangExtAssert.Equal(Right<string, decimal>(12.50m), result);
    }

    [Fact]
    public void ParseAmount_GarbageAmount_ReturnsLeft()
    {
        var result = ExpenseParser.ParseAmount("12.50USD");
        
        LangExtAssert.Equal(Left<string, decimal>("Bad amount: '12.50USD'"), result);
    }

    [Fact]
    public void Either_Match_And_IfLeft()
    {
        var ok = ExpenseParser.ParseAmount("3")
                              .Match(Right: a => $"Ok {a}",
                                     Left: e => $"Err {e}");

        var bad = ExpenseParser.ParseAmount("x")
                               .Match(Right: a => $"Ok {a}",
                                      Left: e => $"Err {e}");

        var fallback = ExpenseParser.ParseAmount("x").IfLeft(0m);
        
        Assert.Equal("Ok 3", ok);
        Assert.Equal($"Err Bad amount: 'x'", bad);
        Assert.Equal(0m, fallback);
    }
}