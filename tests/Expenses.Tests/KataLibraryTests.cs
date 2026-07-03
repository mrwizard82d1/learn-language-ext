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

    [Fact]
    public void BookInLibrary_FindBookByIsbn_ReturnsSome()
    {
        var library = new Library();
        var isbn = new Isbn("978-0-8074-3807-7");
        var book = new Book(isbn, "maiores occaecati sed");
        library.AddBook(book);
        
        var foundBook = library.FindByIsbn(isbn);
        
        LangExtAssert.Equal(Option<Book>.Some(book), foundBook);
    }
}

public class Library
{
    private readonly Dictionary<Isbn, Book> _books = new();

    public void AddBook(Book book)
    {
        _books.Add(book.Id, book);
    }

    public Option<Book> FindByIsbn(Isbn sought)
    {
        return Option<Book>.None;
    }
}

public record Book(Isbn Id, string Title);

