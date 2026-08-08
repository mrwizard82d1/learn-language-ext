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
        
        // No longer necessary ("duplicated" by `result.Match()`  below)
        // Assert.True(result.IsFail);
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

    [Fact]
    public void Validate_AllValid_ReturnsSuccessEntry()
    {
        var result = ExpenseValidation.Validate(9.99m, "Groceries");

        LangExtAssert.Equal(Success<string, ValidatedEntry>(new ValidatedEntry(9.99m, "Groceries")), 
                            result);
    }

    [Fact]
    public void Validate_OneFieldInvalid_FailWithThatSingleError()
    {
        // Only category bad
        var result = ExpenseValidation.Validate(9.99m, "");

        result.Match(Succ: e => Assert.Fail($"Expected failure, got: {e}"),
                     Fail: errors => Assert.Equal("Category is required", errors.Single()));
    }

    [Fact]
    public void Validate_OtherFieldInvalid_FailWithThatSingleError()
    {
        // Only category bad
        var result = ExpenseValidation.Validate(-9.99m, "Groceries");

        result.Match(Succ: e => Assert.Fail($"Expected failure, got: {e}"),
                     Fail: errors => Assert.Equal("Amount must be positive", errors.Single()));
    }

    [Fact]
    public void Match_RendersSuccessOrAllErrors()
    {
        var ok = ExpenseValidation.Validate(1m, "Food")
                                  .Match(Succ: e => $"OK: {e.Category}",
                                         Fail: errors => string.Join("; ", errors));
        
        var bad = ExpenseValidation.Validate(-1m, "")
                                   .Match(Succ:  e => $"OK: {e.Category}",
                                       Fail: errors => string.Join("; ", errors));
        
        Assert.Equal("OK: Food", ok);
        Assert.Equal("Amount must be positive; Category is required", bad);
    }

    [Fact]
    public void Bind_ShortCircuits_ReportingOnlyTheFirstError()
    {
        // Bind: the category check only runs if amount succeeded; consequently,
        // a bad amount hides the bad category entirely.
        var viaBind =
            ExpenseValidation.ValidateAmount(-5m)
                             .Bind(amt => ExpenseValidation.ValidateCategory("")
                                                           .Map(category => new ValidatedEntry(amt, category)));

        viaBind.Match(Succ: entry => Assert.Fail($"Expected failure, got: {entry}"),
                      Fail: errors => Assert.Equal("Amount must be positive", errors.Single()));
    }
}