using LanguageExt;
using static LanguageExt.Prelude;

namespace Expenses;

public sealed class CategoryCatalog
{
    private readonly Dictionary<string, Category> _byName;
    
    public CategoryCatalog(IEnumerable<Category> categories) => 
        _byName = categories.ToDictionary(x => x.Name);
    
    public Option<Category> Find(string name) => Prelude.Optional(_byName.GetValueOrDefault(name));

    public Option<Category> FindParent(Category c) =>
        c.Name switch
        {
            "Groceries" => Prelude.Some(new Category("Food")),
            "Food" => Prelude.Some(new Category("All Spending")),
            _ => Prelude.None
        };
}