# Phase 2 — `Either<L, R>` and `Fin<T>`

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `[ ]`. **You** flip them to `[x]` when complete — I'll just remind you.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Expenses.Tests` Run configuration (Rider) or `dotnet test` (CLI).
- **TDD rhythm:** red → green → refactor. A red must be a *runnable assertion failure*, never a compile error. New types start **inline in the test file**; extracting to `src/Expenses` is the first refactor after green.
- **Assertions:** use `LangExtAssert.Equal(expected, actual)` for any `Option`/`Either`/`Fin` comparison — it renders failures via `ToString()` instead of the `[[…]]` collection dump.

## Goal

Parse raw expense input into typed fields where **failure is a value, not an exception**. Where Phase 1's `Option` said "present or absent" (absence carrying *no* information), `Either`/`Fin` ([new term — see *What `Fin` means*](#what-fin-means)) say "succeeded **with a value**, or failed **with a reason**." Then compose several fallible parses so the **first failure short-circuits** the rest.

This phase also sets up the contrast that drives Phase 3: `Either`/`Fin` **short-circuit** on the first error; `Validation` (Phase 3) **accumulates** all errors. Same problem, two strategies.

## What's actually different here (read this first)

You know this shape from F#'s `Result<'T,'TError>` (`Ok`/`Error`) and from `Either` in Haskell/Scala. The LanguageExt specifics:

| You know | LanguageExt | Why it matters |
|---|---|---|
| F# `Result` is `Ok`/`Error` | `Either<L, R>` is `Left`/`Right`. **Convention: `Right` = success, `Left` = failure** ("right is right"). | `Right` is the "happy path." Mnemonic only — the type is symmetric, but the bias (below) makes `Right` special. |
| F# `Result.map`/`bind` work on the `Ok` track | `Either` is **right-biased**: `Map`/`Bind` transform `Right` and **pass `Left` through untouched**. | This *is* Railway Oriented Programming. `Left` short-circuits the rest of the chain — exactly like `None` did, but now carrying the error. |
| F# ties `Result` to your own error type | **`Fin<A>`** is `Either<Error, A>` with the left type **pinned to LanguageExt's `Error`**. | Use `Fin<A>` when the failure is "an error" (a message / exception / code). Use `Either<L, R>` when `L` is your **own** domain type (an enum, a union, a record). |
| F# `match`/`Option` from Phase 1 | `Either`: `.Match(Right: …, Left: …)`; `Fin`: `.Match(Succ: …, Fail: …)`. Plus `Map`/`Bind`/`IfLeft`/`IfFail`/`BiMap`/`MapLeft`. | Same elevated-world discipline as Phase 1 — stay elevated, unwrap at the end. (See the Wlaschin notes from Phase 1.) |
| exceptions for expected failures | errors are **ordinary return values** you compose. | No `try/catch` for *expected* failures (bad input). Exceptions remain for the *truly exceptional*. `Error.New(ex)` bridges the two when needed. |

The throughline from Phase 1: `Bind` still short-circuits, but where `Option` collapsed to an information-free `None`, `Either`/`Fin` collapse to a `Left`/`Fail` **that remembers why**.

## The `Error` type (for `Fin`)

`LanguageExt.Common.Error` (namespace `using LanguageExt.Common;`):

- `Error.New("bad amount")` — a message error (its `.Code` is `0`, `.Message` is the text).
- `Error.New(exception)` — wraps an exception; `.IsExceptional` is `true`.
- `Error.New(42, "coded")` — message + numeric code.

`Fin<A>` carries one of these on failure, so you get a structured reason for free without inventing your own error type.

## Decisions made

- **Domain:** a raw expense line has `date, amount, category, description`. This phase parses the two *fallible* fields (`date`, `amount`); `category`/`description` ride along as strings (category validation is Phase 3's job).
- **Teach `Either<string, _>` first, then migrate to `Fin<_>`.** We start with `Either<string, decimal>` because a bare `string` left makes the two tracks obvious. Then we adopt `Fin` for the real parser — the idiomatic choice here, since "bad input" is genuinely *an error*. The migration is itself a lesson in the `Either` ↔ `Fin` relationship.
- **Short-circuit only this phase.** Composing the full entry stops at the first bad field. Accumulating *all* field errors is **Phase 3** (`Validation`).
- **Assertions via `LangExtAssert.Equal`** from the first test.
- *(record any other choices you make as you go)*

---

## Candidate test list

Kent Beck style — adjust freely.

- [ ] `ParseAmount` returns `Right(value)` for a valid number
- [ ] `ParseAmount` returns `Left(reason)` for garbage
- [ ] `Match` runs the right branch for `Right` vs `Left`; `IfLeft` supplies a fallback
- [ ] `Map` transforms `Right`, passes `Left` through (right-bias)
- [ ] `Bind` chains a second fallible step; a `Left` anywhere short-circuits
- [ ] `ParseDate` returns `FinSucc(date)` / `FinFail(Error)`; `Match(Succ:, Fail:)`
- [ ] `Error.New(msg)` carries the message; `Error.New(ex)` is `IsExceptional`
- [ ] `ParseEntry` composes date + amount with `Bind`, short-circuiting on the first bad field
- [ ] *(stretch)* conversions: `.ToOption()`, `Either` ↔ `Fin`, `MapLeft`/`BiMap`

---

## Steps

### Step 1 — Red → Green: `ParseAmount` with `Either`  `[x]`

Create `tests/Expenses.Tests/ExpenseParserTests.cs`. Define the parser **inline** with a deliberately-wrong stub so the first test fails on an *assertion*:

```csharp
namespace Expenses.Tests;

using System.Globalization;
using LanguageExt;
using static LanguageExt.Prelude;

public static class ExpenseParser
{
    // Deliberately wrong: always fails, so the first test goes red on an assertion.
    public static Either<string, decimal> ParseAmount(string s) =>
        Left($"bad amount: '{s}'");
}

public class ExpenseParserTests
{
    [Fact]
    public void ParseAmount_ValidNumber_ReturnsRight()
    {
        Either<string, decimal> result = ExpenseParser.ParseAmount("12.50");

        LangExtAssert.Equal(Right<string, decimal>(12.50m), result);
    }
}
```

Run — red on the assertion (`Right(12.50)` vs `Left(...)`). Then make it green:

```csharp
public static Either<string, decimal> ParseAmount(string s) =>
    decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
        ? Right(d)
        : Left($"bad amount: '{s}'");
```

Notes:
- The ternary's two branches are `Right(d)` (`EitherRight<decimal>`) and `Left(...)` (`EitherLeft<string>`); both **target-type** to the method's `Either<string, decimal>` return — same target-typed-conditional mechanism you saw with `Option` in Phase 1.
- In the *test*, `Right<string, decimal>(12.50m)` needs the explicit type args because there's no left value to infer `L` from. (Inside `ParseAmount`, the return type supplies it.)

### Step 2 — Triangulate + consume: `Left`, `Match`, `IfLeft`  `[ ]`

```csharp
[Fact]
public void ParseAmount_Garbage_ReturnsLeft()
{
    Either<string, decimal> result = ExpenseParser.ParseAmount("oops");

    LangExtAssert.Equal(Left<string, decimal>("bad amount: 'oops'"), result);
}

[Fact]
public void Either_Match_And_IfLeft()
{
    string ok  = ExpenseParser.ParseAmount("3").Match(Right: a => $"ok {a}", Left: e => $"err {e}");
    string bad = ExpenseParser.ParseAmount("x").Match(Right: a => $"ok {a}", Left: e => $"err {e}");
    decimal fallback = ExpenseParser.ParseAmount("x").IfLeft(0m);

    Assert.Equal("ok 3", ok);
    Assert.Equal("err bad amount: 'x'", bad);
    Assert.Equal(0m, fallback);
}
```

`Match(Right:, Left:)` is the balanced fold (named args → order-independent, same as Phase 1). `IfLeft(fallback)` is the `Either` analogue of `Option.IfNone` — extract the `Right`, or this fallback on `Left`. (There's a symmetric `IfRight` too.)

### Step 3 — Right-bias: `Map` and `Bind` short-circuit on `Left`  `[ ]`

```csharp
[Fact]
public void Either_Map_TransformsRight_PassesLeftThrough()
{
    Either<string, decimal> r = ExpenseParser.ParseAmount("10").Map(a => a * 2);
    Either<string, decimal> l = ExpenseParser.ParseAmount("x").Map(a => a * 2);

    LangExtAssert.Equal(Right<string, decimal>(20m), r);
    LangExtAssert.Equal(Left<string, decimal>("bad amount: 'x'"), l);   // Map never ran; Left passed through
}

[Fact]
public void Either_Bind_ChainsFallibleStep_AndShortCircuits()
{
    // a second fallible step: amount must be positive
    Either<string, decimal> Positive(decimal a) =>
        a > 0 ? Right(a) : Left($"not positive: {a}");

    LangExtAssert.Equal(Right<string, decimal>(5m), ExpenseParser.ParseAmount("5").Bind(Positive));
    LangExtAssert.Equal(Left<string, decimal>("not positive: -1"), ExpenseParser.ParseAmount("-1").Bind(Positive));
    LangExtAssert.Equal(Left<string, decimal>("bad amount: 'x'"), ExpenseParser.ParseAmount("x").Bind(Positive)); // first failure wins
}
```

This is Railway Oriented Programming concretely: `Map` for a *plain* transform (`A → B`) on the success track, `Bind` for a *fallible* step (`A → Either<L, B>`). Any `Left` skips the rest and carries its reason to the end. (Exactly the Phase 1 `Option` short-circuit, now with an error payload. Decision rule from the Phase 1 "two worlds" notes applies unchanged.)

### Step 4 — Introduce `Fin<T>`: `ParseDate`, and migrate `ParseAmount`  `[ ]`

`Fin<A>` = `Either<Error, A>`. Add `using LanguageExt.Common;` for `Error`. New fallible parse:

```csharp
public static Fin<DateOnly> ParseDate(string s) =>
    DateOnly.TryParse(s, CultureInfo.InvariantCulture, out var dt)
        ? FinSucc(dt)
        : FinFail<DateOnly>(Error.New($"bad date: '{s}'"));
```

```csharp
[Fact]
public void ParseDate_Fin_SuccAndFail()
{
    LangExtAssert.Equal(FinSucc(new DateOnly(2026, 6, 4)), ExpenseParser.ParseDate("2026-06-04"));

    Fin<DateOnly> failed = ExpenseParser.ParseDate("nope");
    Assert.True(failed.IsFail);
    string reason = failed.Match(Succ: d => d.ToString(), Fail: e => e.Message);
    Assert.Equal("bad date: 'nope'", reason);
}
```

Then **migrate `ParseAmount` to `Fin`** — the idiomatic choice for the real parser, since bad input is genuinely *an error*:

```csharp
public static Fin<decimal> ParseAmount(string s) =>
    decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
        ? FinSucc(d)
        : FinFail<decimal>(Error.New($"bad amount: '{s}'"));
```

You'll need to update the Step 1–3 tests to `Fin` (`FinSucc(...)` instead of `Right(...)`, `Match(Succ:, Fail:)`, `IfFail` instead of `IfLeft`). **Keep one or two of the `Either<string,_>` tests** (rename the method, e.g. `ParseAmountEither`) so the `Either` lessons stay exercised — your call. This migration is deliberate: it makes the `Either` ↔ `Fin` relationship concrete (`Fin` is just `Either` with `L = Error`).

> When to use which, restated: **`Fin`** when the left is "an error" (message/exception/code — the common case). **`Either<L,R>`** when the left is your own domain type and you want the compiler to track it.

### Step 5 — Compose the entry with `Bind` (short-circuit on first bad field)  `[ ]`

The payoff: combine multiple fallible parses into one. Add the result type and a composing parse:

```csharp
public sealed record ExpenseEntry(DateOnly Date, decimal Amount, string Category, string Description);

public static Fin<ExpenseEntry> ParseEntry(string date, string amount, string category, string description) =>
    ParseDate(date).Bind(d =>
    ParseAmount(amount).Map(a =>
        new ExpenseEntry(d, a, category, description)));
```

```csharp
[Fact]
public void ParseEntry_AllValid_ReturnsSucc()
{
    Fin<ExpenseEntry> result = ExpenseParser.ParseEntry("2026-06-04", "12.50", "Groceries", "Milk");

    LangExtAssert.Equal(
        FinSucc(new ExpenseEntry(new DateOnly(2026, 6, 4), 12.50m, "Groceries", "Milk")),
        result);
}

[Fact]
public void ParseEntry_ShortCircuitsOnFirstBadField()
{
    // bad date → fails before amount is even parsed
    Fin<ExpenseEntry> badDate = ExpenseParser.ParseEntry("nope", "12.50", "Groceries", "Milk");
    Assert.Equal("bad date: 'nope'", badDate.Match(Succ: _ => "", Fail: e => e.Message));

    // good date, bad amount → fails at amount
    Fin<ExpenseEntry> badAmount = ExpenseParser.ParseEntry("2026-06-04", "oops", "Groceries", "Milk");
    Assert.Equal("bad amount: 'oops'", badAmount.Match(Succ: _ => "", Fail: e => e.Message));
}
```

The nested `Bind`/`Map` reads as: "parse the date; *then* parse the amount; *then* build the entry." Each step is `Fin`, so a `Fail` anywhere stops the chain and carries its `Error` out — you never build an `ExpenseEntry` from a bad field. **Note the short-circuit cost:** if *both* date and amount are bad, you only ever hear about the date. That limitation is precisely what `Validation` fixes in Phase 3.

> **Foreshadow (Phase 5):** that nested `Bind`/`Map` pyramid is exactly what LINQ query syntax flattens — `from d in ParseDate(date) from a in ParseAmount(amount) select new ExpenseEntry(...)`. We'll get there; seeing the explicit form first makes the sugar obvious.

### Step 6 — Refactor: extract to production code  `[ ]`

Green ⇒ promote. Move into `src/Expenses`:
- `src/Expenses/ExpenseEntry.cs` — the `record` (namespace `Expenses`).
- `src/Expenses/ExpenseParser.cs` — the static class (`using LanguageExt; using LanguageExt.Common; using static LanguageExt.Prelude;`, namespace `Expenses`).
- Delete the inline copies from the test file; ensure `using Expenses;` is present.
- Re-run the full suite (Phase 1 tests + smoke canaries included). Green = clean refactor.

### Step 7 — Conversions / interop  `[ ]` *(short)*

Pin how these types interconvert — you'll need this at boundaries:

```csharp
[Fact]
public void Conversions_BetweenFin_Either_Option()
{
    // Fin/Either -> Option : failure becomes None (the reason is dropped)
    Assert.True(ExpenseParser.ParseAmount("x").ToOption().IsNone);
    Assert.Equal(Some(5m), ExpenseParser.ParseAmount("5").ToOption());

    // MapLeft / BiMap : transform the error track (Either<string,_> shown)
    Either<int, decimal> coded =
        ExpenseParser.ParseAmountEither("x").MapLeft(msg => msg.Length);   // string error -> int code
    Assert.True(coded.IsLeft);
}
```

(Adjust to whichever `Either`-returning method you kept in Step 4.) The lesson: moving *down* the type ladder is lossy — `Fin`/`Either` → `Option` throws the reason away (`None`). Moving across (`MapLeft`, `BiMap`) lets you reshape the error track without touching the success track.

---

## Stretch (optional)

- **`BiMap`** — transform both tracks at once: `e.BiMap(Right: r => …, Left: l => …)`.
- **`Match` with effects** — the `Action`-branch form for side effects (logging the error) vs the value-returning form.
- **`@catch` / error recovery** — explore how `Fin` supports fallback-on-error patterns.
- **A domain error type** — replace `Error.New(string)` in one parse with your own `record ParseError(...)` via `Either<ParseError, _>`, to feel when a custom `L` beats `Fin`.
- **Source skim** — open `Either<L,R>` and `Fin<A>` in 4.4.9; confirm `Fin` really is `Either<Error,_>`-shaped underneath.

---

## Notes & questions

### What `Fin` means

`Fin` is a **new term** — it has no direct F#/Clojure equivalent (F# uses `Result<'T,'TError>` = `Ok`/`Error`; there's no type called "Fin"). So here's the verified background, for future-me reading top to bottom.

**Author's own doc comment** on the `Fin<A>` type (Paul Louth / louthy), verbatim:

> "Equivalent of `Either<Error, A>`. Called `Fin` because this should be used as the **concrete result of a computation**."

So:
- **Meaning (author-stated):** `Fin` = the **concrete / final result of a computation** — either `Succ(value)` or `Fail(Error)`.
- **Shape:** equivalent to `Either<Error, A>`. Cases are **`Succ` / `Fail`**; the error is pinned to **`LanguageExt.Common.Error`**.
- **Where the name comes from in the wider library:** when you *run* an effect (`Eff<A>` / `Aff<A>` — the "deep end" this tutorial skips), the result you get back is a `Fin<A>`. It's literally "what's left when the computation **finishes**." But we use it standalone as a plain synchronous *result-or-error* type.
- **Mental model for this phase:** `Fin<DateOnly>` reads as *"parsing's concrete result: a `DateOnly`, or an `Error`."*

**Honest caveat on etymology:** I initially glossed `Fin` as "finish / French *fin* = the end." That reading is *plausible and almost certainly the intent* — but the author never literally writes "finish"; the only on-record statement is "concrete result of a computation." So treat "finish/final" as a helpful interpretation, not a quoted definition.

**When `Fin` vs `Either<L,R>`:** `Fin` when the left is "an error" (message/exception/code — the common case). `Either<L,R>` when the left is your own domain type and you want the compiler to track that specific type.

Sources (verified 2026-06): louthy's `Fin.cs` doc comment (gist `a402c57393b99915e845a37db36252cd`); wiki ["How to deal with side effects"](https://github.com/louthy/language-ext/wiki/How-to-deal-with-side-effects) ("The `Fin` monad is equivalent to `Either<Error, A>`" and "When the `Aff` and `Eff` monads run, they result in a `Fin<A>`"). This is the **v4.x** lineage (our 4.4.9).

-
