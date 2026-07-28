# Phase 2 — `Either<L, R>` and `Fin<T>`

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `[ ]`. **You** flip them to `[x]` when complete — I'll just remind you.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** Rider's built-in test runner ("Run All Tests from Solution") or `dotnet test` (CLI).
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

- [x] `ParseAmount` returns `Right(value)` for a valid number
- [x] `ParseAmount` returns `Left(reason)` for garbage
- [x] `Match` runs the right branch for `Right` vs `Left`; `IfLeft` supplies a fallback
- [x] `Map` transforms `Right`, passes `Left` through (right-bias)
- [x] `Bind` chains a second fallible step; a `Left` anywhere short-circuits
- [x] `ParseDate` returns `FinSucc(date)` / `FinFail(Error)`; `Match(Succ:, Fail:)`
- [x] `Error.New(msg)` carries the message; `Error.New(ex)` is `IsExceptional`
- [x] `ParseEntry` composes date + amount with `Bind`, short-circuiting on the first bad field
 - [x] *(stretch)* conversions: `.ToOption()`, `Either` ↔ `Fin`, `MapLeft`/`BiMap`

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

### Step 2 — Triangulate + consume: `Left`, `Match`, `IfLeft`  `[x]`

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

### Step 3 — Right-bias: `Map` and `Bind` short-circuit on `Left`  `[x]`

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

### Step 4 — Introduce `Fin<T>`: `ParseDate`, and migrate `ParseAmount`  `[x]`

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

### Step 5 — Compose the entry with `Bind` (short-circuit on first bad field)  `[x]`

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

### Step 6 — Refactor: extract to production code  `[x]`

Green ⇒ promote. Move into `src/Expenses`:
- `src/Expenses/ExpenseEntry.cs` — the `record` (namespace `Expenses`).
- `src/Expenses/ExpenseParser.cs` — the static class (`using LanguageExt; using LanguageExt.Common; using static LanguageExt.Prelude;`, namespace `Expenses`).
- Delete the inline copies from the test file; ensure `using Expenses;` is present.
- Re-run the full suite (Phase 1 tests + smoke canaries included). Green = clean refactor.

### Step 7 — Conversions / interop  `[x]` *(short)*

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

### The two-track model: `Map` vs `MapLeft` vs `Bind` (Either)

An `Either` is a **two-track railway**: a *success* track (`Right`) and a *failure* track (`Left`). Three verbs, each with one precise job:

| Verb | Track it touches | What it does | Leaves the other track… |
|---|---|---|---|
| `Map` | **success** (Right) | transform the success value (`A → B`) | untouched (a `Left` passes through) |
| `MapLeft` | **failure** (Left) | transform the error value (`L → M`) | untouched (a `Right` passes through) |
| `Bind` | **success** (Right) | run the *next fallible step* (`A → Either<L,B>`); flatten | if already `Left`, **skip** — short-circuit |

Mental one-liner: **`Map`/`MapLeft` *relabel* a track without leaving it; `Bind` *advances* the success track to the next fallible step (or short-circuits if we've already failed).**

**Why `MapLeft` matters — reconciling heterogeneous errors (the kata Task 5 lesson):** to `Bind` two fallible steps into one chain, they must share **one** error type. When step 1 fails with `Error` (from a `Fin`, via `.ToEither()` → `Either<Error, _>`) and step 2 fails with a domain `BorrowError`, you use `MapLeft` to *translate each step's error into a common union* (`CheckoutError`) so the tracks line up:

```csharp
RequireBook(rawIsbn)                                  // Fin<Book>
    .ToEither()                                        // Either<Error, Book>
    .MapLeft(e  => (CheckoutError) new NotFound(e))    // Either<CheckoutError, Book>   ← relabel error track
    .Bind(book => Borrow(book, member)                 // Either<BorrowError, Loan>
                    .MapLeft(be => (CheckoutError) new CannotBorrow(be)));  // Either<CheckoutError, Loan>
```

- The `(CheckoutError)` casts (or an explicit-return-type lambda, `CheckoutError (e) => …`) **widen** each union case to the base type, so both `MapLeft`s produce `Either<CheckoutError, _>` and `Bind` can thread them.
- `Bind` (not `Map`) because `Borrow` returns an *already-wrapped* value; `Map` would nest it (`Either<…, Either<…>>`). `Bind` = `Map` + flatten (Phase-1 `Flatten`/`join` note).
- This preserves **typed** errors end to end (vs. flattening everything to a string `Error`) — the reason to unify on a domain union rather than collapse to `Fin`. Trade-off recorded because it's subtle and takes a few reps to feel; that's expected.

Same three verbs recur for `Option`, `Fin`, `Validation` — only `Either` exposes both `Map` (right) and `MapLeft` (left) prominently because it's the two-sided one.

### Design philosophy: OO cohesion vs FP's "100 functions on one data structure"

**My musing (verbatim):**

> When I first started programming, I was strongly influence by the Wirth's book on Pascal and separating "programs" from "data". When I first encountered object-oriented code, after applying Wirth's "maxim" but using C, I had "discovered" that, often, a single data structure was "surrounded" by several functions that operated on that data structure (with additional influence from SICP). Putting these things together made sense (consider Bob Martin's idea that "things that change together are commited together"). Functional programming is different from but related to Wirth's idea, but, seemingly, combined with Wirth's next big idea of "modules" - except that Clojure demonstrates that one can apply the same functional ideas to **many, different (but related) data structures**. I think there is something there, but it feels just beyond my grasp....

**Response — three named ideas I was circling:**

- **Perlis's epigram (from SICP's foreword):** *"It is better to have 100 functions operate on one data structure than 10 functions operate on 10 data structures."* OO trends toward **10×10** (many bespoke *types*, each with its own methods); FP/Clojure toward **100×1** (a few general data shapes + a large library of generic functions over them). "Same functional ideas across many data structures" = feeling the 100×1 world.

- **The arc, named:**
  - *Wirth:* **separate** algorithm from data (`Algorithms + Data Structures = Programs`).
  - *OO (my C→objects discovery):* **re-couple** them — cohesion **"by type"** (data + its methods); unit of modularity = the **type/object**. (Bob Martin's "changes together" pushes here.)
  - *FP/Clojure:* **separate again**, but re-cohere at a higher level — **"an abstraction + all the generic functions over it"**; unit of modularity = the **operation/abstraction**; one function serves *many* concrete structures.
  - So OO groups code **by the type it belongs to**; FP groups code **by the operation and the abstraction it targets.** Two different axes of cohesion, not enemies.

- **The Expression Problem (Wadler) — the "just beyond my grasp" bit.** Two axes of change: add a new *type* vs add a new *operation*. **OO makes adding types easy, operations hard; FP makes adding operations easy, types hard** — duals; you pay on opposite axes. Languages that aim to be open on *both*: **Clojure** (protocols / multimethods over plain data) and **Haskell/LanguageExt** (**typeclasses** — `Functor`/`Monad`/`Foldable`: a generic function written once against the abstraction, many concrete types opt in).

**Why it matters here (I'm living in it):** `Map`, `Bind`, `Match`, `Fold` are the "100 functions"; `Option`, `Either`, `Fin`, `Validation`, `Seq`, `Map<K,V>` are the "many data structures." The reason `Map`/`Bind` **transfer** across all of them — kata `Bind` intuition working on `Either` *and* `Fin` *and* `Option` — is that they're all **Functors/Monads**, and the ops are written to that *abstraction*, not to any one type. That transfer *is* Perlis's 100×1 + typeclasses, concrete in C#. It's also why this tutorial says "learn `Map`/`Bind`/`Match` once, apply everywhere."

**One-line synthesis:** OO cohesion = "one type, its operations" (group by data); FP/Clojure cohesion = "one operation, its abstraction, many types" (group by behavior over a shared shape) — and the Expression Problem says each buys easy extension on the axis the other makes hard. LanguageExt is the FP side ported into C#: a small set of universal operations over a family of "elevated" data types.

**References:**
- Philip Wadler, *"The Expression Problem"* (1998) — the canonical statement. <https://homepages.inf.ed.ac.uk/wadler/papers/expression/expression.txt>
- Alan J. Perlis, *"Epigrams on Programming"* (1982, ACM SIGPLAN Notices) — source of "100 functions on one data structure…". (Perlis also wrote SICP's foreword; the epigram itself is from this paper — a small correction to my earlier "SICP foreword" attribution.)
- Rich Hickey, talks *"Simple Made Easy"* (2011) and *"The Value of Values"* (2012) — the Clojure "plain data + generic functions" philosophy.

### Combining OO and FP in one codebase (is the axis-duality a reason *not* to mix?)

**My reflection (verbatim):**

> Object-oriented programming and functional programming "focus on" one of two different axes. Do these axes mean that combining the two approaches in one code base is, generally, a bad idea (requires the human reader to "grok" both paradigms)? I think, generally, that conclusion is too string [strong]. I have professional found success in using both LINQ and Rx - both of which rely heavily on using "functions" but typically in the context of an object-oriented code base. I, and I believe others, have found this kind of code understandable and maintainable - although I recognize it uses the "simplest" form of functional programming (functions are first class). It seems like the ideas of combining functions (composition) and other ideas from functional programming are, perhaps, more difficult for "OO-practioners" to fully grok, but are the high-leverage points of functional programming.

**Response:**
- **"Too strong" is right.** The Expression Problem describes an *extensibility trade-off within a design* (add-types-easy vs add-ops-easy) — **not** a warning against mixing paradigms. "Dual axes" ≠ "don't mix."
- **Mixing is mainstream and usually a strength — each at its best granularity:** OO for large-scale *structure* (components, boundaries, encapsulation, wiring); FP for *computation in the small* (pipelines, composition, immutability). Different scales → complementary.
- **LINQ/Rx is the sweet spot:** first-class functions + composition applied *locally* where data flows, without restructuring the OO app — FP's highest-leverage, lowest-friction export.
- **Cost of mixing scales with (a) how *deep* into FP you go and (b) how *inconsistent* the boundaries are — not with mixing itself.** Friction: mutation/effects sneaking into "pure" pipelines; and forcing the *deep end* (monads/typeclasses/HKT) on a team that hasn't opted in (why this tutorial excludes it). `Option`/`Either`/`Fin` sit in the middle.
- **The twist:** **LINQ query syntax *is* monadic composition** — `from…from…select` desugars to `SelectMany` = **`Bind`**. So the "high-leverage composition" that's hard-to-grok is something I've *already* done for years, specialized to `IEnumerable`. The leap is *generalizing* `Bind` from sequences (LINQ) to `Option`/`Either`/`Fin` (LanguageExt) — **Phase 5** shows LINQ query syntax over these types.
- **Net:** combine deliberately — right tool at the right scale, clear boundaries, FP *depth* matched to team fluency. Multi-paradigm is a feature.

### FP and system architecture (functional core / imperative shell; Clean Architecture)

**My reflection (verbatim):**

> I've had some success (the Orchid Python API available on GitHub) in combining the two ideas using the "functional core, imperative shell" idea (a bit different, I think, from the idea you expressed of "FP for computation" (but maybe not really different as I think about it more)). I think, too, I've been very intrigued by ideas that architectures like Martin's "Clean Architecture" are amenable to "functional in part - OO in part" but a bit less certain. Although my experience using Rx for the Orchid product successfully introduced functional programming into a code base whose domain layer was mostly implemented using OO.

**Response:**
- **FC/IS *is* "FP for computation," named at architecture scale.** Gary Bernhardt's *"Boundaries"* (2012): a **pure functional core** (all logic — deterministic, no I/O, trivially testable) wrapped by a **thin imperative shell** (I/O, DB, UI — the effects). So "maybe not really different" is correct — same principle, architectural name.
- **The elevated types are the core's vocabulary.** `Option`/`Either`/`Fin`/`Validation` let the core express absence/failure *as values* → the core stays **pure and total**; the **shell** runs the core and `Match`es the result at the boundary. ROP + FC/IS = one picture: pure railway core, effectful shell.
- **Clean Architecture is highly FP-amenable** — the Dependency Rule (deps point inward; frameworks/I/O outside; business logic independent) is FC/IS **at module scale**. Inner rings (entities, use cases) = functional core (pure domain, immutable types, elevated-value-returning); outer rings (adapters, frameworks) = imperative shell. So "functional inner rings, OO outer rings" is natural.
- **Nuances (why "less certain" is fair):** FC/IS is fine-grained (function purity), Clean Architecture coarse-grained (module dependency) — they *rhyme*, and you nest FC/IS *within* a use case. And FP often expresses a "port" as a **function type** (or effects-as-data) rather than an OO interface + DI — Mark Seemann calls FC/IS the *functional take on Ports & Adapters* ("dependency rejection": keep the core pure, decide effects in the shell). Compatible with Clean Architecture, different flavor.
- **The architectural takeaway:** biggest FP wins aren't "rewrite in FP" — they're (1) **purify the core** (testable/reason-able), (2) **make failure/absence explicit as values** (composable, visible in signatures), (3) **push effects to a thin shell.** Orchid/Rx (reactive edges on an OO domain) is this in practice — FP introduced where it's highest-leverage, no rewrite.

**References:**
- Gary Bernhardt, *"Boundaries"* (2012) — functional core, imperative shell.
- Scott Wlaschin, *Domain Modeling Made Functional* (2018) — DDD/architecture done functionally; also the ROP source.
- Mark Seemann — *"Functional architecture is Ports and Adapters"* / *"Dependency rejection"* (blog) — FC/IS as the functional take on Clean/Hexagonal.
- Robert C. Martin, *Clean Architecture* (2017) — the OO baseline; Alistair Cockburn's Hexagonal (Ports & Adapters) is its FP-friendly sibling.

#### Addendum — Hexagonal Architecture (Ports & Adapters): sources & the FP link

**Cockburn's canonical material — the site *moved*, it's not lost:**
- New official home: <https://alistaircockburn.com/Articles/Hexagonal-Architecture>
- Original personal-wiki page (still resolves): <https://alistair.cockburn.us/hexagonal-architecture/>
- History: idea originated **~1994** (Portland Pattern Repository / c2 wiki); Cockburn renamed it **"Ports and Adapters"** and published the canonical write-up in **2005** (exact day/month unverified; "hexagon" = his *drawing convention*, not "six of anything").
- Book: ***Hexagonal Architecture Explained***, Alistair Cockburn & Juan Manuel Garrido de Paz, **2024** (ISBN 978-1-7375197-8-2). Companion site (Garrido de Paz): <https://jmgarridopaz.github.io/> (incl. a Cockburn interview).

**Why it's the "FP-friendly sibling" (sourced):**
- **Mark Seemann, *"Functional architecture is Ports and Adapters"* (2016-03-18)** — <https://blog.ploeh.dk/2016/03/18/functional-architecture-is-ports-and-adapters/>. The clean citation for the claim: in FP a function's *type signature* declares pure vs impure, so `IO`/effects are **forced to the boundary (adapters)** while pure domain logic forms the core — structurally identical to Ports & Adapters. Good functional design lands in P&A "by default" (a "pit of success").
- Seemann, ***"Dependency rejection"*** (2017-02-02, <https://blog.ploeh.dk/2017/02/02/dependency-rejection/>) & "From DI to dependency rejection" (2017-01-27) — FP replaces DI with composition of pure + impure functions (the *"impureim sandwich"*): keep the core pure, decide effects at the edges. (The "ports as functions, not interfaces" nuance from above.)
- Seemann NDC talk, *"Functional architecture — the pits of success"* — <https://www.youtube.com/watch?v=US8QG9I1XW0>.

**Related comparisons / treatments:**
- Johan Martinsson, *"Hexagonal architecture vs Functional core / Imperative shell"* — <http://martinsson-johan.blogspot.com/2021/01/hexagonal-architecture-vs-functional.html> (a direct head-to-head of the two).
- Gary Bernhardt, *"Boundaries"* — <https://www.destroyallsoftware.com/talks/boundaries> (origin of "functional core, imperative shell").
- Scott Wlaschin, *"Six approaches to dependency injection"* — <https://fsharpforfunandprofit.com/posts/dependencies/> (FP's take on ports/dependencies at the boundary).
- *Increment*, *"A primer on functional architecture"* — <https://increment.com/software-architecture/primer-on-functional-architecture/>.

-
