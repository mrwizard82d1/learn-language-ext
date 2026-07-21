# Refresher Kata 01 — Mini Library

> A **self-directed** refresher in a *different* domain from the expense tracker. The point isn't to follow steps — it's to **solve a small problem and reach for `Option` / `Either` / `Fin` where they fit**, using your own Notes and asking questions as needed.

## Progress / resume point

- **Task 1 — ✅ done.** `KataLibraryTests.cs`: `record Book`, `Library` with `AddBook` + `FindByIsbn(isbn) => Optional(_books.GetValueOrDefault(sought))`, hit + miss tests green. `Isbn` is a `using Isbn = string;` alias (not distinct — deferred). Chose to keep `Dictionary` + `Optional` bridge now, revisit `Map<K,V>` in Phase 4 (staggered learning). Noted: the `Optional(GetValueOrDefault)` idiom relies on `Book` being a *reference* type; a `record struct` would break the `None` case.
- **Task 2 — ✅ done.** `Isbn` is a **`readonly record struct`** with private ctor + validating `static Fin<Isbn> Create(string)` (empty/whitespace, length==13, all-`char.IsAsciiDigit`; stores normalized dash-stripped value). Strong `Isbn` threaded through `Book`/`Library`/tests. Includes 1a fix (strip from *trimmed* value + padded-valid test) and item-4 polish. 30 tests green. Failure tests assert via `IsFail`+`Match` (not `Equal` — `Fin` ignores `Error` in equality). *(Also: the whole project migrated xUnit v3/MTP → xUnit 2/VSTest on 2026-07-10.)*

- **Task 3 — ✅ done.** `RequireBook(string) → Fin<Book>` via `Isbn.Create(...).Bind(isbn => FindByIsbn(isbn).ToFin(Error.New(...)))`. Three tests (present→`Succ`, absent→`Fail`"no book", malformed→`Fail` with the *parse* message — which **proves the short-circuit**: lookup never runs). Explored a point-free `Match` "verbose twin" (`git` history at `99ca10f`), proved equivalent by the same green tests, then reverted.

  **Key insight (Bind/ToFin = the Match twin, unrolled):**
  - Outer `Match(Succ: f, Fail: FinFail<Book>)` — where `Fail` just re-wraps the error *unchanged* — **is** `.Bind(f)`.
  - Inner `Match(Some: FinSucc, None: () => FinFail(err))` — **is** `Option.ToFin(err)`.
  - So the combinators aren't magic; they're the exact plumbing, factored out. **Heuristic:** if a branch only re-shapes/re-wraps without adding logic, a combinator already does it — that's the tell to collapse it. "Explicit-to-learn, then collapse" is a fine workflow.

- **Task 4 — ✅ done.** `Borrow(Book, Member) → Either<BorrowError, Loan>` on `Library`. `BorrowError` enum = `{ AlreadyOnLoan, MemberAtLimit }` (dropped the `Uncategorized` stub). `Library` tracks active loans in a `Dictionary<Isbn, Loan>`; two sequential guards (book already out → `AlreadyOnLoan`; member's loan count `>= MaxActiveLoansPerMember(3)` → `MemberAtLimit`) then record + `Right(loan)`. Types: `MemberId(int)` record struct, `Member(MemberId Id, string Name)`, `Loan(MemberId BorrowerId, Isbn BookIsbn)` (identity vs role naming). 36 tests green.

  **Key insight:** `Either<L,R>` **compares its `Left` value** in equality (verified), so a domain error can be asserted directly: `LangExtAssert.Equal(Left(BorrowError.AlreadyOnLoan), result)` — *unlike* `Fin`, which ignores its `Error` (there you need `IsFail`+`Match`). Precise assertability is a reason `Either<L,R>` beats `Fin` for domain failures. Sequential guards = first-failure-wins short-circuit (accumulating all needs `Validation`).

- **Task 5 — ✅ core done.** `Checkout(string rawIsbn, Member member) → Either<CheckoutError, Loan>` (chose **Option B**: unify on a typed `CheckoutError` union — `NotFound(Error)` | `CannotBorrow(BorrowError)` — to preserve typed errors). Chain: `RequireBook(rawIsbn).ToEither().MapLeft(e => (CheckoutError)new NotFound(e)).Bind(book => Borrow(book, member).MapLeft(be => (CheckoutError)new CannotBorrow(be)))`. Three tests: happy→`Right(loan)`; bad ISBN→`Left(NotFound)` (asserted by *shape*, `is NotFound`, since `Error` compares by message); already-on-loan→`Left(CannotBorrow(AlreadyOnLoan))` (asserted by exact value — `Either` compares `Left`). **39 green.**

  **Key insights (now in `phases/phase-02-either-fin.md` Notes → "two-track model"):** `Map` = success track, `MapLeft` = failure track, `Bind` = advance-success-or-short-circuit (= Map + flatten). The `(CheckoutError)` casts widen union cases to the base so both `MapLeft`s produce `Either<CheckoutError,_>` and `Bind` threads them. Larry worked hard to articulate the control flow — landed it over ~3 commits ("very dimly" → correct in his own words).

### ▶ Resume here

- [ ] **(Optional) Task 5 closer:** `Describe(Either<CheckoutError, Loan>) → string` — a friendly `Match` (`"Loaned … to …"` / failure message; `switch` on the `CheckoutError` union). Gentle presentation step; then the kata is fully done.
- [ ] **Kata essentially complete** — every type (`Option`/`Either`/`Fin`) + the hard heterogeneous reconciliation covered. **Natural main-line next: Phase 3 (`Validation`)** — accumulate *all* errors instead of short-circuiting on the first (the direct sequel to the kata's short-circuit theme). Phase 3 doc not yet authored.

## Rules of engagement

- **Less guided on purpose.** Below are *requirements* and gentle hints about where each concept tends to fit — not code to copy. You design the types and signatures.
- **Lean on your Notes.** `phases/phase-01-option.md` and `phases/phase-02-either-fin.md` Notes sections are your reference (the bridge, Map/Bind/Flatten, two-worlds, "What `Fin` means", conversions).
- **TDD if you like** (it's how you've worked), but optional here — this is practice, not the main line. `LangExtAssert.Equal` is available.
- **Ask me anything** — "is this idiomatic?", "which type should this return?", "why won't this compile?" That dialogue *is* the learning.
- **Keep it isolated** from the expense work — see Setup.

## Setup (low friction)

Easiest: add a new test file in the existing test project so LanguageExt + `LangExtAssert` just work:
`tests/Expenses.Tests/KataLibraryTests.cs` (define the kata types *inline* there, same as you did for `CategoryCatalog`). It's throwaway practice — delete it later, or keep it. (If you'd rather a clean project, say so and I'll set one up.)

Top of the file you'll likely want:
```csharp
namespace Expenses.Tests;

using LanguageExt;
using LanguageExt.Common;          // Error
using static LanguageExt.Prelude;  // Some/None/Right/Left/FinSucc/FinFail/Optional…
```

## The domain

A tiny library: books you can look up, member checkouts, and the rules around borrowing. Suggested shapes (yours to change):

- `Book` — an ISBN, title, author.
- `Member` — an id, name.
- `Loan` — which book, which member (and maybe a due date).
- A **catalog** of books and a record of **active loans** (a `Dictionary`/list is fine — persistent collections are a later phase).

---

## Tasks

Each task names the concept it tends to invite. Resist over-thinking — the *first* type that fits is usually right.

### 1. Look something up that might not be there → `Option`
`FindByIsbn(string isbn) → Option<Book>`. The catalog has a few books; a miss is **absence, not an error** (no reason needed).
*Recall:* what's the idiomatic bridge when your backing store is a `Dictionary` returning `null` on a miss?

### 2. Parse / validate input that can fail *with a reason* → `Fin`
Graduate `Isbn` from the `using`-alias into a real **`readonly record struct Isbn`** with a **smart constructor**:
`public static Fin<Isbn> Create(string raw)` — validate (strip hyphens/whitespace; require exactly 13 digits) and return `FinSucc(new Isbn(clean))` or `FinFail<Isbn>(Error.New("ISBN must be 13 digits; got 10"))`. Make the raw ctor **private** so `Create` is the only door in — "parse, don't validate": holding an `Isbn` *proves* it's valid.
*(Simpler alternative: a free function `ParseIsbn(string raw) → Fin<Isbn>`. Same validation, but it can't stop a caller from `new`-ing an invalid `Isbn` elsewhere. We're going with `Isbn.Create` for the stronger guarantee.)*
*Recall:* failure is a **value** here (an `Error`), not a thrown exception — needs `using LanguageExt.Common;` for `Error`. Why `Fin` rather than `Option`? Because "absent, no reason" isn't enough; you want to say *why* it's invalid.
*Caveat:* a `record struct` can't close the `default(Isbn)` hole (its `Value` would be `null`); the private ctor stops `new Isbn("garbage")`, not `default`. Good enough for the kata.

### 3. Turn "absent" into "an error" → `Option` → `Fin`
`RequireBook(string isbn) → Fin<Book>`: reuse `FindByIsbn`, but for a flow where a missing book *is* a failure, convert the `None` into a `Fail(Error.New("no book with isbn …"))`.
*Recall:* this is the eliminator boundary — you're deciding what absence *means* in this context. (Look at how `Option` converts toward `Either`/`Fin` in your Phase-2 conversions note.)

### 4. A domain operation with *your own* error type → `Either<L, R>`
`Borrow(Book, Member) → Either<BorrowError, Loan>`, where **`BorrowError` is a type you define** (an `enum` or a small `record`/union): e.g. `AlreadyOnLoan`, `MemberAtLimit` (say, max 3 active loans).
*Recall:* this is the case where `Either<L,R>` earns its keep over `Fin` — the failure is a **domain concept you want the compiler to track**, not just a message.

### 5. Compose the whole flow, short-circuiting on the first failure → `Bind`
`Checkout(string rawIsbn, Member member) → …`: chain it —
**parse the ISBN → require the book → borrow it for the member** — so the first failure stops the rest and carries its reason out. Finish by `Match`-ing to a friendly `string` result ("Loaned 'Dune' to Ada" / the failure reason).
*Recall:* `Map` vs `Bind` decision; staying in the elevated world; unwrap only at the end.
*The interesting wrinkle (lean in / ask me):* your steps currently return **different** elevated types — `Fin` (parse, require) and `Either<BorrowError,_>` (borrow). To `Bind` them into one chain you must **reconcile** them onto a single type. How would you do it? (Hint: `MapLeft` / `BiMap` to align error types, or pick one error representation for the whole flow. This is a real design decision — a great thing to discuss.)

---

## Stretch

- **Return a book:** `Return(isbn, member) → Fin<Unit>` (or `Either`), failing if that loan doesn't exist. Exercises `Unit` and another fallible op.
- **`Filter`:** only allow borrowing books whose title isn't on a blocklist — `Some`→`None` / a `Left`.
- **Accumulate (peek at Phase 3):** what if `Checkout` should report *all* problems at once (bad ISBN *and* member-at-limit) instead of stopping at the first? Note why `Bind` can't do that — that's exactly what `Validation` (Phase 3) is for. Don't implement it; just feel the gap.

## When you're done

There's no answer key by design — ping me to review your solution, sanity-check idioms, or compare against how I'd write it. Then back to the main line: **Phase 2, Step 3**.

--- 

## Notes and questions

** Idiomatic code for translating a C# value that could return `null` into an `Option` type. **

The `GetValueOrDefault()` call returns the sought book if it is present. If `null` is returned, the compiler
will return the "default" for the type, `Book`. Since `Book` is a record, the default value will again be
`null`. The call to `Optional` will translate these two values into `Option.Some<Book>` if the value is
**not** `null` and into `Option<Book>.None` if no such book exists.

We could have defined a `Book` to be a `record struct` instead of a `record`. The `record` type, under the 
hood is actually a .NET class which has a default value of `null`. If we had used a `record struct`. we 
would actually have a `struct` "under the hood" and the default value would not be `null` but a record with
all "bits" initialized to zero.

Sneak peak. we'll review this choice in Phase 4 when we translate the `Dictionary` to using `Map<K, V>`
from LanguageExt.
