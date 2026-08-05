namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public static class ExpenseValidation
{
    // Deliberately wrong: always fails so first test fails on assertion
    public static Validation<string, decimal> ValidateAmount(decimal amount) =>
        amount > 0
            ? Success<string, decimal>(amount)
            : Fail<string, decimal>("Amount must be positive");
}
public class ExpenseValidationTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void ValidateAmount_Positive_ReturnsSuccess()
    {
        var result = ExpenseValidation.ValidateAmount(12.50m);
        
        LangExtAssert.Equal(Success<string, decimal>(12.50m), result);
    }

    [Fact]
    public void ValidateAmount_NonPositive_ReturnsFail()
    {
        var result = ExpenseValidation.ValidateAmount(-1m);
        
        Assert.True(result.IsFail);
    }
}