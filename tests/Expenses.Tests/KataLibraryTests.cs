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

        var isbn = Isbn.Create("978-0-7766-5519-2").ThrowIfFail();
        var foundBook = library.FindByIsbn(isbn);

        LangExtAssert.Equal(Option<Book>.None, foundBook);
    }

    [Fact]
    public void BookInLibrary_FindBookByIsbn_ReturnsSome()
    {
        var library = new Library();
        var isbn = Isbn.Create("978-0-8074-3807-7").ThrowIfFail();
        var book = new Book(isbn, "maiores occaecati sed");
        library.AddBook(book);

        var foundBook = library.FindByIsbn(isbn);

        LangExtAssert.Equal(Option<Book>.Some(book), foundBook);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void EmptyOrAllWhitespaceIsbnText_CreateIsbn_ReturnsFinFail(string candidateIsbn)
    {
        var maybeIsbn = Isbn.Create(candidateIsbn);

        Assert.Multiple(
            () => Assert.True(maybeIsbn.IsFail),
            () => Assert.Contains($"ISBN can be neither empty nor all whitespace: '{candidateIsbn}'.",
                                  maybeIsbn.Match(Succ: _ => "",
                                      Fail: e => e.Message))
            );
    }

    [Fact]
    public void TenCharacterIsbn_CreateIsbn_ReturnsFinFail()
    {
        const string candidateIsbn = "0-601-17942-4";
        var maybeIsbn = Isbn.Create(candidateIsbn);

        const string candidateIsbnNoDashes = "0601179424";
        Assert.Multiple(
            () => Assert.True(maybeIsbn.IsFail),
            () => Assert.Contains($"ISBN must be 13 characters long: {candidateIsbnNoDashes}.",
                                  maybeIsbn.Match(Succ: _ => "",
                                                  Fail: e => e.Message))
        );
    }

    [Fact]
    public void IsbnWithNonDigitCharacter_CreateIsbn_ReturnsFinFail()
    {
        const string candidateIsbn = "012345678901a";
        var maybeIsbn = Isbn.Create(candidateIsbn);

        Assert.Multiple(
            () => Assert.True(maybeIsbn.IsFail),
            () => Assert.Equal($"ISBN must only contain digits: {candidateIsbn}.",
                               maybeIsbn.Match(Succ: _ => "",
                                               Fail: e => e.Message))
        );
    }

    [Fact]
    public void ValidIsbnText_CreateIsbn_ReturnsFin()
    {
        const string candidateIsbn = "978-0-06-346001-0";
        const string expectedIsbnValue = "9780063460010";
        var isbn = Isbn.Create(candidateIsbn).ThrowIfFail();

        LangExtAssert.Equal(expectedIsbnValue, isbn.Value);
    }

    [Fact]
    public void PaddedButValidIsbnText_CreateIsbn_ReturnsFin()
    {
        const string candidateIsbn = " 978-0-7237-7584-3\r";
        var isbn = Isbn.Create(candidateIsbn).ThrowIfFail();

        const string expectedIsbnValue = "9780723775843";
        LangExtAssert.Equal(expectedIsbnValue, isbn.Value);
    }

    [Fact]
    public void ValidIsbnAndBookPresent_RequireBook_ReturnsFinBook()
    {
        var library = new Library();
        
        const string validIsbnText = "978-0-590-22380-5";
        var isbn =  Isbn.Create(validIsbnText).ThrowIfFail();
        var book = new Book(isbn, "sapiente recusandae aliquam");
        
        library.AddBook(book);
        
        var actual = library.RequireBook(validIsbnText);
        
        LangExtAssert.Equal(FinSucc(book), actual);
    }

    [Fact]
    public void ValidIsbnAndBookAbsent_RequireBook_ReturnsFinFailWithNoBookMessage()
    {
        var library = new Library();
        
        const string validIsbnText = "978-1-08-085983-7";
        
        var actual = library.RequireBook(validIsbnText);
        
        const string expectedIsbnText = "9781080859837";
        Assert.Multiple(
            () => Assert.True(actual.IsFail),
            () => Assert.Contains($"No book with ISBN {expectedIsbnText} found.",
                                  actual.Match(Succ: _ => "", 
                                               Fail: e => e.Message))
        );
    }

    [Fact]
    public void InvalidIsbn_RequireBook_ReturnsFinFailWithInvalidIsbnMessage()
    {
        var library = new Library();
        
        const string invalidIsbnText = "978-1-03-838901-";
        
        var actual = library.RequireBook(invalidIsbnText);
        
        const string expectedInvalidIsbnText = "978103838901";
        Assert.Multiple(
            () => Assert.True(actual.IsFail),
            () => Assert.Contains($"ISBN must be 13 characters long: {expectedInvalidIsbnText}.",
                                  actual.Match(Succ: _ => "", 
                                               Fail: e => e.Message))
        );
    }

    [Fact]
    public void BookAvailable_LoanBook_ReturnsRight()
    {
        // Given
        const string soughtIsbnText = "978-1-63614-253-1";
        var soughtIsbn = Isbn.Create(soughtIsbnText).ThrowIfFail();
        var soughtBook = new Book(soughtIsbn, "repellendus rerum natus");
        
        var library = new Library();
        library.AddBook(soughtBook);

        // When
        var memberId = new MemberId(3293);
        const string  memberName = "Brenda Liu";
        var member = new Member(memberId, memberName);
        var result = library.Borrow(soughtBook, member);
        
        // Then
        const string trimmedIsbnText = "9781636142531";
        LangExtAssert.Equal(Right<Loan>(new Loan(memberId, soughtIsbn)), result);
    }
}

public class Library
{
    private readonly Dictionary<Isbn, Book> _books = new();

    public void AddBook(Book book)
    {
        _books.Add(book.Id, book);
    }

    public Either<BorrowError, Loan> Borrow(Book book, Member member) =>
        Right(new Loan(member.Id, book.Id));

    // Idiomatic code for translating a C# value that could return `null` into an `Option` type.
    //
    // The `GetValueOrDefault()` call returns the sought book if it is present. If `null` is returned, the compiler
    // will return the "default" for the type, `Book`. Since `Book` is a record, the default value will again be
    // `null`. The call to `Optional` will translate these two values into `Option.Some<Book>` if the value is
    // **not** `null` and into `Option<Book>.None` if no such book exists.
    //
    // We could have defined a `Book` to be a `record struct` instead of a `record`. The `record` type, under the
    // hood is actually a .NET class which has a default value of `null`. If we had used a `record struct`, we
    // would actually have a `struct` "under the hood" and the default value would not be `null` but a record with
    // all "bits" initialized to zero.
    //
    // Sneak peek: we'll review this choice in Phase 4 when we translate the `Dictionary` to using `Map<K, V>`
    // from LanguageExt.
    //
    public Option<Book> FindByIsbn(Isbn sought)  =>
        Optional(_books.GetValueOrDefault(sought));

    public Fin<Book> RequireBook(string validIsbnText) =>
        Isbn.Create(validIsbnText)
            .Bind(isbn => FindByIsbn(isbn).ToFin(Error.New($"No book with ISBN {isbn.Value} found.")));
}

public readonly record struct Isbn
{
    public string Value { get;  } // Define a read-only property
    private Isbn(string value) => Value = value; // Initialize this property in a **private** constructor

    // Factory method to construct an instance from a string.
    public static Fin<Isbn> Create(string candidateIsbn)
    {
        var trimmedCandidateIsbn = candidateIsbn.Trim();
        if (string.IsNullOrWhiteSpace(trimmedCandidateIsbn))
        {
            return FinFail<Isbn>(Error.New($"ISBN can be neither empty nor all whitespace: '{candidateIsbn}'."));
        }

        var noDashCandidateIsbn = trimmedCandidateIsbn.Replace("-", ""); // Remove dashes
        if (noDashCandidateIsbn.Length != 13) // "new" ISBN only
        {
            return FinFail<Isbn>(Error.New($"ISBN must be 13 characters long: {noDashCandidateIsbn}."));
        }

        if (!noDashCandidateIsbn.All(char.IsAsciiDigit))
        {
            return FinFail<Isbn>(Error.New($"ISBN must only contain digits: {noDashCandidateIsbn}."));
        }

        return FinSucc<Isbn>(new Isbn(noDashCandidateIsbn));
    }
}

public record Book(Isbn Id, string Title);

public enum BorrowError
{
    Uncategorized,
}

public readonly record struct MemberId(int Value);

public record Member(MemberId Id, string Name);

public record Loan(MemberId BorrowerId, Isbn BookIsbn);

