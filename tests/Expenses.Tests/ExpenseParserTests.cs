namespace Expenses.Tests;

using System.Globalization;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

public static class ExpenseParser
{
    // Correct behavior.
    public static Either<string, decimal> ParseAmount(string s) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) 
            ? Right(d) 
            : Left($"Bad amount: '{s}'");

    public static Fin<DateOnly> ParseDate(string s) =>
        DateOnly.TryParse(s, CultureInfo.InvariantCulture, out var dt)
            ? FinSucc(dt)
            : FinFail<DateOnly>(Error.New($"Bad date: '{s}'"));
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

    [Fact]
    public void Either_Map_TransformsRight_PassesLeftThrough()
    {
        // Because `ParseAmount("10")` "succeeds", `Map` is then invoked on the result.
        var ok = ExpenseParser.ParseAmount("10").Map(a => a * 2);
        // But `ParseAmount("x") "fails" (takes the left branch of the
        // `Either`), the `Map` function is **never even invoked**
        var bad = ExpenseParser.ParseAmount("x").Map(a => a * 2);
        
        LangExtAssert.Equal(Right<string, decimal>(20m), ok);
        LangExtAssert.Equal(Left<string, decimal>("Bad amount: 'x'"), bad);
    }

    [Fact]
    public void Either_Bind_ChainsFallibleCheck_AndShortCircuits()
    {
        // Create a second step **that may fail** (the amount must be positive)
        Either<string, decimal> Positive(decimal a) =>
            a > 0 ? Right(a) : Left($"Not positive: {a}");
        
        LangExtAssert.Equal(Right<string, decimal>(5m), ExpenseParser.ParseAmount("5").Bind(Positive));
        LangExtAssert.Equal(Left<string, decimal>("Not positive: -3"), 
                            ExpenseParser.ParseAmount("-3")
                                         .Bind(Positive));
        LangExtAssert.Equal(Left<string, decimal>("Bad amount: 'x'"),
                                ExpenseParser.ParseAmount("x")
                                             .Bind(Positive));
    }

    [Fact]
    public void ParseDate_Fin_SuccessAndFail()
    {
        LangExtAssert.Equal(FinSucc(new DateOnly(1993, 1, 30)), 
            ExpenseParser.ParseDate("1993-01-30"));

        var failed = ExpenseParser.ParseDate("2017-Fed-16");
        Assert.True(failed.IsFail);

        var reason = failed.Match(
            Succ: d => d.ToLongDateString(),
            Fail: e => e.Message
            );
        Assert.Equal("Bad date: '2017-Fed-16'", reason);
    }
}