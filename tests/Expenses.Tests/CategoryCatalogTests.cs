using Xunit.Sdk;

namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public class CategoryCatalogTests
{
    private readonly Dictionary<string, Category> _byName;

    [Fact]
    public void Find_KnownCategory_ReturnsSome()
    {
        var groceries = new Category("Groceries");
        var catalog = new CategoryCatalog([groceries]);

        var result = catalog.Find("Groceries");
        
        Assert.Equal(Some(groceries), result);
    }

    [Fact]
    public void Find_UnknownCategory_ReturnsNone()
    {
        var catalog = new CategoryCatalog([new Category("Groceries")]);

        var result = catalog.Find("Rent");
        
        Assert.True(result.IsNone);
    }

    [Fact]
    public void IfNone_OnMiss_SuppliesFallbackValue()
    {
        var catalog = new CategoryCatalog([new Category("Groceries")]);
        var uncategorized = new Category("Uncategorized");

        var result = catalog.Find("Rent").IfNone(uncategorized);
        
        Assert.Equal(uncategorized, result);
    }

    [Fact]
    public void IfNone_Factor_IsSkippedOnHit_AndRunOnMiss()
    {
        var catalog = new CategoryCatalog([new Category("Groceries")]);
        var factoryCalls = 0;

        Category MakeDefault()
        {
            factoryCalls++;
            return new Category("Uncategorized");
        }

        // Runs the function `MakeDefault()` if **not** found.
        var hit = catalog.Find("Groceries").IfNone(MakeDefault);
        Assert.Equal(new Category("Groceries"), hit);
        Assert.Equal(0, factoryCalls); // The point: thunk did **not** run on the `Some` path
        
        // Similarly, runs `MakeDefault()` if and only if `None` returned by `Find`
        var miss = catalog.Find("Rent").IfNone(MakeDefault);
        Assert.Equal(new Category("Uncategorized"), miss);
        Assert.Equal(1, factoryCalls); // Ran exactly once only on the `None` path
    }
    
    // Demonstrate that `IfSome(Action<A>)` only fires on `Some` and not `None` and that the side effect, `Action<A>`
    // only occurs if a match **is** found.
    [Fact]
    public void IfSome_RunsAction_OtherwiseDoesNotRun()
    {
        var catalog = new CategoryCatalog([new Category("Groceries")]);
        var seen = new List<string>();

        catalog.Find("Groceries").IfSome(c => seen.Add(c.Name)); // runs **and** adds an item
        catalog.Find("Rent").IfSome(c => seen.Add(c.Name)); // runs but adds **no** item
        
        Assert.Equal("Groceries", Assert.Single(seen)); // sees a list with a single item, "Groceries"
    }

    [Fact]
    public void Match_Some_SelectsTheSomeBranchAction()
    {
        var catalog = new CategoryCatalog([new Category("Groceries")]);

        string label = catalog.Find("Groceries")
                              .Match(Some: c => $"Found: {c.Name}",
                                     None: () => "Not found");
        
        Assert.Equal("Found: Groceries", label);
    }
}

public sealed record Category(string Name);

public sealed class CategoryCatalog
{
    private readonly Dictionary<string, Category> _byName;
    
    public CategoryCatalog(IEnumerable<Category> categories) => 
        _byName = categories.ToDictionary(x => x.Name);
    
    public Option<Category> Find(string name) => Optional(_byName.GetValueOrDefault(name));
}
