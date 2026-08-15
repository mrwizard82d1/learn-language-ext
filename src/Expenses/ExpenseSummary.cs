using LanguageExt;

namespace Expenses;

public static class ExpenseSummary
{
    public static Map<string, decimal> SummarizeByCategory(Seq<ExpenseEntry> entries) =>
        entries.Fold(Prelude.Map<string, decimal>(),
                     (acc, e) => acc.AddOrUpdate(e.Category,
                                                 Some: cur => cur + e.Amount,
                                                 None: () => e.Amount));

    public static Option<decimal> TotalFor(Map<string, decimal> summary, string category) =>
        summary.Find(category);
}