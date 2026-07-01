namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude; // "Undecorated" Some/None/Right/Left/FinSucc/FinFail/Optional...

// A "type alias" 
using Isbn = string;

public class KataLibraryTests
{
    [Fact]
    public void NoSuchBookInLibrary_FindBookByIsbn_ReturnsNone()
    {
        var library = new Library();

        var isbn = new Isbn("1-07-040578-7");
        var foundBook = library.FindByIsbn(isbn);
        
        LangExtAssert.Equal(Option<Book>.None, foundBook);
    }
}

public class Library
{
    public Option<Book> FindByIsbn(Isbn sought)
    {
        return Option<Book>.None;
    }
}

public record Book(Isbn Id, string Title);

