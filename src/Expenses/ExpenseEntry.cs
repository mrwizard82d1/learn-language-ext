namespace Expenses;

public sealed record ExpenseEntry(DateOnly Date, decimal Amount, string Category, string Description);