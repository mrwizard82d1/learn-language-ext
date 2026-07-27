namespace Expenses.Tests;

using System.Globalization;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

public static class ExpenseParser
{
    // Correct behavior.
    public static Either<string, decimal> ParseAmountEither(string s) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) 
            ? Right(d) 
            : Left($"Bad amount: '{s}'");

    public static Fin<decimal> ParseAmount(string s) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) 
            ? FinSucc(d) 
            : FinFail<decimal>(Error.New($"Bad amount: '{s}'"));

    public static Fin<decimal> ParseAmountException(string s)
    {
        try
        {
            return FinSucc(decimal.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture));
        }
        catch (FormatException fe)
        {
            return FinFail<decimal>(Error.New(fe));
        }
    }

    public static Fin<DateOnly> ParseDate(string s) =>
        DateOnly.TryParse(s, CultureInfo.InvariantCulture, out var dt)
            ? FinSucc(dt)
            : FinFail<DateOnly>(Error.New($"Bad date: '{s}'"));

    public static Fin<ExpenseEntry> ParseEntry(string date, string amount, string category, string description)
    {
        return FinFail<ExpenseEntry>("Placeholder error");
    }
}
public class ExpenseParserTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);
    
    [Fact]
    public void ParseAmount_ValidNumber_ReturnsRight()
    {
        var result = ExpenseParser.ParseAmountEither("12.50");
        
        LangExtAssert.Equal(Right<string, decimal>(12.50m), result);
    }

    [Fact]
    public void ParseAmount_GarbageAmount_ReturnsLeft()
    {
        var result = ExpenseParser.ParseAmountEither("12.50USD");
        
        LangExtAssert.Equal(Left<string, decimal>("Bad amount: '12.50USD'"), result);
    }

    [Fact]
    public void Either_Match_And_IfLeft()
    {
        var ok = ExpenseParser.ParseAmountEither("3")
                              .Match(Right: a => $"Ok {a}",
                                     Left: e => $"Err {e}");

        var bad = ExpenseParser.ParseAmountEither("x")
                               .Match(Right: a => $"Ok {a}",
                                      Left: e => $"Err {e}");

        var fallback = ExpenseParser.ParseAmountEither("x").IfLeft(0m);
        
        Assert.Equal("Ok 3", ok);
        Assert.Equal($"Err Bad amount: 'x'", bad);
        Assert.Equal(0m, fallback);
    }

    [Fact]
    public void Either_Map_TransformsRight_PassesLeftThrough()
    {
        // Because `ParseAmount("10")` "succeeds", `Map` is then invoked on the result.
        var ok = ExpenseParser.ParseAmountEither("10").Map(a => a * 2);
        // But `ParseAmount("x") "fails" (takes the left branch of the
        // `Either`), the `Map` function is **never even invoked**
        var bad = ExpenseParser.ParseAmountEither("x").Map(a => a * 2);
        
        LangExtAssert.Equal(Right<string, decimal>(20m), ok);
        LangExtAssert.Equal(Left<string, decimal>("Bad amount: 'x'"), bad);
    }

    [Fact]
    public void Either_Bind_ChainsFallibleCheck_AndShortCircuits()
    {
        // Create a second step **that may fail** (the amount must be positive)
        Either<string, decimal> Positive(decimal a) =>
            a > 0 ? Right(a) : Left($"Not positive: {a}");
        
        LangExtAssert.Equal(Right<string, decimal>(5m), ExpenseParser.ParseAmountEither("5").Bind(Positive));
        LangExtAssert.Equal(Left<string, decimal>("Not positive: -3"), 
                            ExpenseParser.ParseAmountEither("-3")
                                         .Bind(Positive));
        LangExtAssert.Equal(Left<string, decimal>("Bad amount: 'x'"),
                                ExpenseParser.ParseAmountEither("x")
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

    [Fact]
    public void ParseAmount_Fin_SuccessAndFail()
    {
        LangExtAssert.Equal(FinSucc(951.44m), 
                            ExpenseParser.ParseAmount("951.44"));

        var failed = ExpenseParser.ParseDate("139.40 TOP");
        Assert.True(failed.IsFail);

        var reason = failed.Match(
            Succ: d => d.ToLongDateString(),
            Fail: e => e.Message
        );
        Assert.Equal("Bad date: '139.40 TOP'", reason);
    }

    [Fact]
    public void ParseAmountException_Fin_IsExceptional()
    {
        var failed = ExpenseParser.ParseAmountException("636.70MAD");
        Assert.True(failed.IsFail);

        var err = failed.Match(
            Succ: _ => Error.New("Totally unexpected"),
            Fail: e => e
        );
        Assert.True(err.IsExceptional);
    }

    [Fact]
    public void ParseEntry_AllValid_ReturnsSuccess()
    {
        Fin<ExpenseEntry> result = ExpenseParser.ParseEntry("2022-10-14", "887.04", "saepe", "quam");
        
        LangExtAssert.Equal(
            FinSucc(new ExpenseEntry(new DateOnly(2022, 10, 14), 
                                     887.04m, "saepe",  "quam")),
            result);
    }
}

public sealed record ExpenseEntry(DateOnly Date, decimal Amount, string Category, string Description);