using LanguageExt;

namespace Expenses.Tests;

using static LanguageExt.Prelude;

public class CategoryCatalogTests
{
    private readonly CategoryCatalog _catalog;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public CategoryCatalogTests() => 
        _catalog = new CategoryCatalog([new Category("Groceries")]);

    [Fact]
    public void Find_KnownCategory_ReturnsSome()
    {
        var groceries = new Category("Groceries");

        var result = _catalog.Find("Groceries");
        
        LangExtAssert.Equal(groceries, result);
    }

    [Fact]
    public void Find_UnknownCategory_ReturnsNone()
    {
        var result = _catalog.Find("Rent");
        
        Assert.True(result.IsNone);
    }

    [Fact]
    public void IfNone_OnMiss_SuppliesFallbackValue()
    {
        var uncategorized = new Category("Uncategorized");

        var result = _catalog.Find("Rent").IfNone(uncategorized);
        
        Assert.Equal(uncategorized, result);
    }

    [Fact]
    public void IfNone_Factor_IsSkippedOnHit_AndRunOnMiss()
    {
        var factoryCalls = 0;

        Category MakeDefault()
        {
            factoryCalls++;
            return new Category("Uncategorized");
        }

        // Runs the function `MakeDefault()` if **not** found.
        var hit = _catalog.Find("Groceries").IfNone(MakeDefault);
        Assert.Equal(new Category("Groceries"), hit);
        Assert.Equal(0, factoryCalls); // The point: thunk did **not** run on the `Some` path
        
        // Similarly, runs `MakeDefault()` if and only if `None` returned by `Find`
        var miss = _catalog.Find("Rent").IfNone(MakeDefault);
        Assert.Equal(new Category("Uncategorized"), miss);
        Assert.Equal(1, factoryCalls); // Ran exactly once only on the `None` path
    }
    
    // Demonstrate that `IfSome(Action<A>)` only fires on `Some` and not `None` and that the side effect, `Action<A>`
    // only occurs if a match **is** found.
    [Fact]
    public void IfSome_RunsAction_OtherwiseDoesNotRun()
    {
        var seen = new List<string>();

        _catalog.Find("Groceries").IfSome(c => seen.Add(c.Name)); // runs **and** adds an item
        _catalog.Find("Rent").IfSome(c => seen.Add(c.Name)); // runs but adds **no** item
        
        Assert.Equal("Groceries", Assert.Single(seen)); // sees a list with a single item, "Groceries"
    }

    [Fact]
    public void Match_Some_SelectsTheSomeBranchAction()
    {
        var label = _catalog.Find("Groceries")
                            .Match(Some: c => $"Found: {c.Name}",
                                   None: () => "Not found");
        
        Assert.Equal("Found: Groceries", label);
    }

    [Fact]
    public void Map_ProjectsName_AndIsNoOpOnMiss()
    {
        var hit = _catalog.Find("Groceries").Map(c => c.Name);
        LangExtAssert.Equal(Some("Groceries"), hit);
        
        var miss = _catalog.Find("Rent").Map(c => c.Name);
        LangExtAssert.Equal(None, miss);
    }

    [Fact]
    public void Map_OfAnOptionReturningFunc_Nests_WhereBindFlattens()
    {
        // Map wraps the function's (already-`Option`) return in **another** `Option`:
        var nested = _catalog.Find("Groceries").Map(_catalog.FindParent);
        LangExtAssert.Equal(Some(Some(new Category("Food"))), nested); // nested: Option<<Option<...>>
        
        // Bind flattens it - Map + flatten
        var flat = _catalog.Find("Groceries").Bind(_catalog.FindParent);
        LangExtAssert.Equal(Some (new Category("Food")), flat); // flat - Option<Category>
    }

    [Fact]
    public void Bind_ChainsLookups_AndShortCircuits()
    {
        // hit -> hit -> hit: the happy path runs to the end
        var twoUp = 
            _catalog.Find("Groceries")
                    .Bind(_catalog.FindParent)
                    .Bind(_catalog.FindParent);
        Assert.Equal(Some(new Category("All Spending")), twoUp);
        
        // miss at the **first** step -> the rest is skipped entirely
        var missChain = 
            _catalog.Find("Rant")
                    .Bind(_catalog.FindParent)
                    .Bind(_catalog.FindParent);
        Assert.True(missChain.IsNone);
        
        // hit -> hit -> hit -> miss ("All Spending" has no parent) -> None
        var hitThenMiss =
            _catalog.Find("Groceries")
                    .Bind(_catalog.FindParent)
                    .Bind(_catalog.FindParent)
                    .Bind(_catalog.FindParent);
        Assert.True(hitThenMiss.IsNone);
    }

    [Fact]
    public void Optional_TreatsNullAsNone()
    {
        string? missing = null;
        const string present = "x";
        
        // `Optional(x).IsNone` returns true iff x == `null`
        Assert.True(Optional(missing).IsNone);
        
        // `Optional(x).Equals(Some(x))` iff x != `null`
        Assert.Equal(Some("x"), Optional(present));
    }

    [Fact]
    public void Filter_KeepsSomeIfPredicatePasses_ElseNone()
    {
        var kept = _catalog.Find("Groceries").Filter(c => c.Name.StartsWith("Groceries"));
        Assert.Equal(Some(new Category("Groceries")), kept);
        
        // Value existed but failed the predicate results in `None`
        var rejected = _catalog.Find("Groceries").Filter(c => c.Name.Length > 100);
        Assert.True(rejected.IsNone);
        
        // If already `None`, stays `None`
        var miss = _catalog.Find("Rent").Filter(_ => true);
        Assert.True(miss.IsNone);
    }

    [Fact]
    public void Case_EnablesCSharpPatternSwitch()
    {
        // ReSharper disable once MoveLocalFunctionAfterJumpStatement
        string Describe(Option<Category> o) =>
        o.Case switch
        {
            Category c => $"Found '{c.Name}'",
            _ => "None"
        };
        
        Assert.Equal("Found 'Groceries'", Describe(_catalog.Find("Groceries")));
        Assert.Equal("None", Describe(_catalog.Find("Rent")));
    }

    [Fact]
    public void Option_InteropsWithNullableValuesAndEnumerable()
    {
        // `ToNullable`: VALUE types only (Option<int> -> int?). Some -> value; None -> null.
        Assert.Equal(5, Some(5).ToNullable());
        Assert.Null(((Option<int>)None).ToNullable());
        
        // Option as a sequence with 0-or-1 item
        Assert.Equal(["Groceries"], _catalog.Find("Groceries").Map(c => c.Name).AsEnumerable());
        Assert.Empty(_catalog.Find("Rent").AsEnumerable());
    }
}