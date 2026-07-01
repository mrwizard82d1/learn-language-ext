# Refresher Kata 01 — Mini Library

> A **self-directed** refresher in a *different* domain from the expense tracker. The point isn't to follow steps — it's to **solve a small problem and reach for `Option` / `Either` / `Fin` where they fit**, using your own Notes and asking questions as needed.

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
`ParseIsbn(string raw) → Fin<string>` (or introduce a small `Isbn` wrapper type). Rules, e.g.: strip hyphens/whitespace; it must be exactly 13 digits. On failure, return a `Fail` whose `Error` says *why* ("ISBN must be 13 digits, got 10").
*Recall:* failure is a **value** here, not a thrown exception. Why `Fin` rather than `Option` for this?

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
