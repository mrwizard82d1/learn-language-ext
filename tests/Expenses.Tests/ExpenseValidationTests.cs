namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public sealed record ValidatedEntry(decimal Amount, string Category);

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

    public static Validation<string, ValidatedEntry> Validate(decimal amount, string category) =>
        (ValidateAmount(amount), ValidateCategory(category))
        .Apply((a, c) => new ValidatedEntry(a, c));
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
        result.Match(
            Succ: v => Assert.Fail($"Expected failure but got: {v}"),
            Fail: errors => Assert.Equal("Category is required", errors.Single())
            );
    }

    [Fact]
    public void Validate_AllFieldsInvalid_AccumulatesAllErrors()
    {
        var result = ExpenseValidation.Validate(-5m, "\t\n");

        result.Match(
            Succ: v => Assert.Fail($"Expected failure but got: {v}"),
            Fail: errors =>
            {
                Assert.Equal(2, errors.Count);
                Assert.Contains("Amount must be positive", errors);
                Assert.Contains("Category is required", errors);
            });
    }
}