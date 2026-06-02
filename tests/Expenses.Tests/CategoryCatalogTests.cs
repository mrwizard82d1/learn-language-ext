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
}

public sealed record Category(string Name);

public sealed class CategoryCatalog
{
    private readonly Dictionary<string, Category> _byName;
    
    public CategoryCatalog(IEnumerable<Category> categories) => 
        _byName = categories.ToDictionary(x => x.Name);
    
    public Option<Category> Find(string name) => Optional(_byName.GetValueOrDefault(name));
}
