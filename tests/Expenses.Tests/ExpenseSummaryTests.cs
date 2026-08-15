namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public class ExpenseSummaryTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void Map_Find_ReturnsSomeForHit_NoneForMiss()
    {
        var m = Map(("Food", 8m), ("Gas", 10m));
        
        LangExtAssert.Equal(Some(8m), m.Find("Food"));
        LangExtAssert.Equal(Option<decimal>.None, m.Find("Rent"));
    }

    [Fact]
    public void Add_IsNonDestructive()
    {
        var original = Map(("Food", 8m));
        var updated = original.Add("Gas", 10m);
        
        Assert.Equal(1, original.Count); // original untouched
        Assert.Equal(2, updated.Count); // new map has both
        LangExtAssert.Equal(Option<decimal>.None, original.Find("Gas"));
    }

    [Fact]
    public void AddOrUpdate_UpdateExisting_InsertsMissing()
    {
        var m = Map(("Food", 5m));
        
        var updated = m.AddOrUpdate("Food", 
                                    Some: cur => cur + 3m, 
                                    None: () => 3m); // 5 -> 8
        var inserted = m.AddOrUpdate("Gas", 
                                     Some: cur => cur + 3m, 
                                     None: () => 3m); // absent -> 3
        
        LangExtAssert.Equal(Some(8m), updated.Find("Food"));
        LangExtAssert.Equal(Some(3m), inserted.Find("Gas"));
    }

    [Fact]
    public void SummarizeByCategory_SumsAmountPerCategory()
    {
        var entries = Seq(
            new ExpenseEntry(new DateOnly(2026, 1, 1), 5m, "Food", "lunch"),
            new ExpenseEntry(new DateOnly(2026, 1, 2), 3m, "Food", "snack"),
            new ExpenseEntry(new DateOnly(2026, 1, 3), 10m, "Gas", "fill-up"));

        var totals = ExpenseSummary.SummarizeByCategory(entries);
        
        LangExtAssert.Equal(Some(8m), totals.Find("Food"));
        LangExtAssert.Equal(Some(10m), totals.Find("Gas"));
        Assert.Equal(2, totals.Count);
    }

    [Fact]
    public void SummarizeByCategory_Empty_ReturnsEmptyMap()
    {
        var totals = ExpenseSummary.SummarizeByCategory(Seq<ExpenseEntry>());
        
        // No `Some(x)` values so the "total" is zero (0)
        Assert.Equal(0, totals.Count);
    }

    [Fact]
    public void TotalFor_ReturnsSomeForKnown_NoneForUnknown()
    {
        var totals =
            ExpenseSummary.SummarizeByCategory(
                Seq(
                    new ExpenseEntry(new DateOnly(2026, 1, 1),
                                     5m,
                                     "Food",
                                     "lunch"),
                    new ExpenseEntry(new DateOnly(2026, 1, 2),
                                     10m,
                                     "Gas",
                                     "fill-up")));
        
        LangExtAssert.Equal(Some(5m), ExpenseSummary.TotalFor(totals, "Food"));
        LangExtAssert.Equal(None, ExpenseSummary.TotalFor(totals, "Rent"));
    }
}
