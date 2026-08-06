using LanguageExt.Common;

namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public static class ExpenseValidation
{
    public static Validation<string, decimal> ValidateAmount(decimal amount) =>
        amount > 0
            ? Success<string, decimal>(amount)
            : Fail<string, decimal>("Amount must be positive");

    public static Validation<string, string> ValidateCategory(string category) =>
        !string.IsNullOrWhiteSpace(category)
            ? Success<string, string>(category)
            : Fail<string, string>("Category is required");
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

    [Fact]
    public void ValidateCategory_NonEmpty_ReturnsSuccess()
    {
        var result = ExpenseValidation.ValidateCategory("Groceries");
        LangExtAssert.Equal(Success<string, string>("Groceries"), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\t")]
    [InlineData(" \t\f")]
    public void ValidateCategory_EmptyCategory_ReturnsFail(string erroneousCategory)
    {
        var result = ExpenseValidation.ValidateCategory(erroneousCategory);
        
        Assert.True(result.IsFail);
        var actualErrors = result.Match(
            Succ: _ => Seq<string>(), // necessary to get everything to compile; should actually never get here
            Fail: errors => errors
            );
        Assert.Equal("Category is required", actualErrors.Single());
    }
}