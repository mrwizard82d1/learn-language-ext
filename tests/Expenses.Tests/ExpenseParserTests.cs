namespace Expenses.Tests;

using System.Globalization;
using LanguageExt;
using static LanguageExt.Prelude;

public static class ExpenseParser
{
    // Deliberately wrong: always fails, so the first test goes red on
    // assertion.
    public static Either<string, decimal> ParseAmount(string s) => 
        Left($"Bad amount: '{s}'");
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
}