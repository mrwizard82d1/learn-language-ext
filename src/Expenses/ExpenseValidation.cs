using LanguageExt;

namespace Expenses;

public static class ExpenseValidation
{
    public static Validation<string, decimal> ValidateAmount(decimal amount) =>
        amount > 0
            ? Prelude.Success<string, decimal>(amount)
            : Prelude.Fail<string, decimal>("Amount must be positive");

    public static Validation<string, string> ValidateCategory(string category) =>
        !string.IsNullOrWhiteSpace(category)
            ? Prelude.Success<string, string>(category)
            : Prelude.Fail<string, string>("Category is required");

    public static Validation<string, ValidatedEntry> Validate(decimal amount, string category) =>
        (ValidateAmount(amount), ValidateCategory(category))
        .Apply((a, c) => new ValidatedEntry(a, c));
}