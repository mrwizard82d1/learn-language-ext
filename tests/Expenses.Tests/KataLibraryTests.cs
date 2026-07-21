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
        LangExtAssert.Equal(Right(new Loan(memberId, soughtIsbn)), result);
    }

    [Fact]
    public void BookAlreadyOnLoan_Borrow_ReturnsLeftAlreadyOnLoan()
    {
        // Given: a book, already borrowed once
        var isbn = Isbn.Create("978-0-09-784633-0").ThrowIfFail();
        var book = new Book(isbn, "saepe atque eos");
        var library = new Library();
        library.AddBook(book);
        library.Borrow(book, new Member(new MemberId(9683), "Michael Edwands"));
        
        // When: someone tries to borrow a book a second time
        var actual = library.Borrow(book, new Member(new MemberId(4176), "Carmen Jackson"));
        
        // Then borrowing "fails" with `AlreadyOnLoan`
        Either<BorrowError, Loan> expected = Left(BorrowError.AlreadyOnLoan);
        LangExtAssert.Equal(expected, actual);
    }

    [Fact]
    public void MemberAtLoanLimit_Borrow_ReturnsLeftMemberAtLimit()
    {
        var library = new Library();
        var member = new Member(new MemberId(6041), "Marc Salazar");
        
        // ReSharper disable once UnusedLocalFunctionReturnValue
        Book AddAndBorrow(string isbnText)
        {
            var b = new Book(Isbn.Create(isbnText).ThrowIfFail(), "Don't care");
            library.AddBook(b);
            library.Borrow(b, member);
            return b;
        }

        AddAndBorrow("978-0-201-48207-2");
        AddAndBorrow("978-0-12-245994-8");
        AddAndBorrow("978-0-459-07654-2");
        
        // Now try to borrow the fourth, different book.
        var fourth = new Book(Isbn.Create("978-0-16-235007-6").ThrowIfFail(), "Don't care");
        library.AddBook(fourth);
        var actual = library.Borrow(fourth, member);
        
        Either<BorrowError, Loan> expected = Left(BorrowError.MemberAtLimit);
        LangExtAssert.Equal(expected, actual);
    }

    [Fact]
    public void BookAvailable_Checkout_BookCheckedOut()
    {
        var library = new Library();
        var isbnText = "978-1-912496-09-9";
        var soughtIsbn = Isbn.Create(isbnText).ThrowIfFail();
        var book = new Book(soughtIsbn, "nam fugit sed");
        library.AddBook(book);

        var member = new Member(new MemberId(5187), "Robert Cook");
        var actual = library.Checkout(isbnText, member);
        
        LangExtAssert.Equal(Right(new Loan(new MemberId(5187), soughtIsbn)), actual);
    }

    [Fact]
    public void BadIsbn_Checkout_ReturnsLeftNotFound()
    {
        var library = new Library();
        var member = new Member(new MemberId(6227), "Johnathan Hamilton");

        var actual = library.Checkout("978-1-970099-05-", member);
        
        // NotFound wraps an Error whose equality is "message-brittle"; therefore, assert the **shape**
        Assert.True(actual.Match(
                        Right: _ => false, 
                        Left: e => e is NotFound));
    }

    [Fact]
    public void BookAlreadyOnLoan_Checkout_ReturnsLeftCannotBorrow()
    {
        var library = new Library();
        var soughtIsbn = "978-1-214-22052-1";
        var isbn = Isbn.Create(soughtIsbn).ThrowIfFail();
        library.AddBook(new Book(isbn, "natus repundiandae nemo"));
        library.Checkout(soughtIsbn, new Member(new MemberId(732), "Cesar Gray"));
        
        var actual = library.Checkout(soughtIsbn,  
                                      new Member(new MemberId(9524), "Ginny Robles"));

        Either<CheckoutError, Loan> expected = 
            Left((CheckoutError)new CannotBorrow(BorrowError.AlreadyOnLoan));
        LangExtAssert.Equal(expected, actual);
    }

    [Fact]
    public void BookCheckedOut_Describe_ReportLoan()
    {
        var library = new Library();
        var member = new Member(new MemberId(3560), "Jamie Bray");
        var isbnText = "978-1-67715-286-5";
        var isbn = Isbn.Create(isbnText).ThrowIfFail();
        var soughtBook = new Book(isbn, "omnis voluptatum beatae");
        library.AddBook(soughtBook);

        var loan = library.Checkout(isbnText, member);

        var description = Library.Describe(loan);
        
        LangExtAssert.Equal($"Loaned 9781677152865 to member 3560", description);
    }

    [Fact]
    public void NoBookWithIsbn_Describe_ReportNotFound()
    {
        var library = new Library();
        var member = new Member(new MemberId(3809), "Lauren Mooney");
        var isbnText = "978-0-694-80337-8";
        var isbn = Isbn.Create(isbnText).ThrowIfFail();
        var soughtBook = new Book(isbn, "est veritatis provident");

        var loan = library.Checkout(isbnText, member);

        var description = Library.Describe(loan);
        
        LangExtAssert.Equal($"Not found: No book with ISBN 9780694803378 found.", description);
    }
}

public class Library
{
    private readonly Dictionary<Isbn, Book> _books = new();
    private readonly Dictionary<Isbn, Loan> _loans = new();

    private const int MaxActiveLoansPerMember = 3;

    public void AddBook(Book book)
    {
        _books.Add(book.Id, book);
    }

    public Either<BorrowError, Loan> Borrow(Book book, Member member)
    {
        if (_loans.ContainsKey(book.Id))
        {
            return Left(BorrowError.AlreadyOnLoan);
        }

        if (_loans.Values.Count(l => l.BorrowerId == member.Id) >= MaxActiveLoansPerMember)
        {
            return Left(BorrowError.MemberAtLimit);
        }

        var loan = new Loan(member.Id, book.Id);
        _loans.Add(book.Id, loan);
        return Right(loan);
    }

    public Either<CheckoutError, Loan> Checkout(string rawIsbn, Member member) =>
        RequireBook(rawIsbn)
            .ToEither()
            .MapLeft(CheckoutError (e) => new NotFound(e))
            .Bind(book => Borrow(book, member)
                      .MapLeft(CheckoutError (be) => new CannotBorrow(be)));

    public static string Describe(Either<CheckoutError, Loan> checkoutResult) =>
        checkoutResult.Match(
            Right: loan => $"Loaned {loan.BookIsbn.Value} to member {loan.BorrowerId.Value}", 
            Left: error => error switch
            {
                NotFound nf => $"Not found: {nf.error.Message}",
                CannotBorrow cb => $"Cannot borrow: {cb.Reason}",
                _ => "Unknown error",
            });

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

        return FinSucc(new Isbn(noDashCandidateIsbn));
    }
}

public record Book(Isbn Id, string Title);

public enum BorrowError
{
    AlreadyOnLoan,
    MemberAtLimit,
}

public abstract record CheckoutError;
public sealed record NotFound(Error error) : CheckoutError;

public sealed record CannotBorrow(BorrowError Reason) : CheckoutError;

public readonly record struct MemberId(int Value);

public record Member(MemberId Id, string Name);

public record Loan(MemberId BorrowerId, Isbn BookIsbn);

