namespace Expenses.Tests;

using LanguageExt;
using LanguageExt.Common; // Error
using static LanguageExt.Prelude; // "Undecorated" Some/None/Right/Left/FinSucc/FinFail/Optional...

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

    [Fact]
    public void BadlyFormedIsbn_CreateIsbn_ReturnsFinFail()
    {
        var candidateIsbn = "";
        
        var maybeIsbn = Isbn.Create(candidateIsbn);

        LangExtAssert.Equal(FinFail<Isbn>(Error.New(candidateIsbn)), maybeIsbn);
    }
}

public class Library
{
    private readonly Dictionary<Isbn, Book> _books = new();

    public void AddBook(Book book)
    {
        _books.Add(book.Id, book);
    }

    // Idiomatic code for translating a C# value that could return `null` into an `Option` type.
    //
    // The `GetValueOrDefault()` call returns the sought book if it is present. If `null` is returned, the compiler
    // will return the "default" for the type, `Book`. Since `Book` is a record, the default value will again be
    // `null`. The call to `Optional` will translate these two values into `Option.Some<Book>` if the value is
    // **not** `null` and into `Option<Book>.None` if no such book exists.
    //
    // We could have defined a `Book` to be a `record struct` instead of a `record`. The `record` type, under the 
    // hood is actually a .NET class which has a default value of `null`. If we had used a `record struct`. we 
    // would actually have a `struct` "under the hood" and the default value would not be `null` but a record with
    // all "bits" initialized to zero.
    //
    // Sneak peak. we'll review this choice in Phase 4 when we translate the `Dictionary` to using `Map<K, V>`
    // from LanguageExt.
    //
    public Option<Book> FindByIsbn(Isbn sought)  => 
        Optional(_books.GetValueOrDefault(sought));
}

public record struct Isbn(string Value)
{
    // Factory method to construct an instance from a string.
    public static Fin<Isbn> Create(string candidateIsbn) => FinFail<Isbn>(Error.New("todo"));
}

public record Book(Isbn Id, string Title);

