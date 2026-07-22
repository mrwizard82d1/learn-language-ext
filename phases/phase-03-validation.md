# Phase 3 — `Validation<F, S>`

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `[ ]`. **You** flip them to `[x]` when complete — I'll just remind you.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** Rider's built-in test runner ("Run All Tests from Solution") or `dotnet test` (CLI).
- **TDD rhythm:** red → green → refactor. A red must be a *runnable assertion failure*, never a compile error. New types start **inline in the test file**; extracting to `src/Expenses` is the first refactor after green.
- **Assertions:** use `LangExtAssert.Equal(expected, actual)` for any wrapper comparison.

## Goal

Validate an expense entry against **several independent field rules** and report **every** failure at once — not just the first. This is the **accumulate** counterpart to Phase 2's **short-circuit**:

- `Either`/`Fin` (`Bind`): stop at the **first** error. Right for a *pipeline* where each step depends on the previous succeeding (you can't borrow a book you couldn't find).
- `Validation` (`Apply`): run **independent** checks and collect **all** failures. Right for "tell the user *everything* wrong with this form at once."

You just felt the short-circuit in your bones (the kata). Phase 3 is its mirror image.

## What's actually different here (read this first)

**The one big idea: a monad (`Bind`) short-circuits; an applicative (`Apply`) accumulates.**

| | `Either`/`Fin` — **monad** (`Bind`) | `Validation` — **applicative** (`Apply`) |
|---|---|---|
| Composition | `Bind` — each step *depends on* the previous value | `Apply` — checks are *independent*, combined after |
| On failure | **short-circuits** at the first error | **accumulates** all errors into a `Seq<F>` |
| Use when | steps are sequential/dependent (parse → lookup → borrow) | checks are independent (validate all fields of a form) |
| Result | `Left`/`Fail` with **one** error | `Fail` with **every** error |

**Why `Bind` *can't* accumulate (the crux):** `Bind(a => f(a))` needs `a` — the *success* of step 1 — to even run `f`. If step 1 failed, there's no `a`, so `f` never runs, so its potential error is *unknowable*. An applicative sidesteps this: the checks **don't depend on each other**, so all of them run, and their failures are gathered. Dependency is exactly what forces short-circuiting; independence is what allows accumulation.

**F# / FsToolkit anchor:** this is `Validation` / the `validation { }` computation expression in FsToolkit.ErrorHandling — applicative `Result` that accumulates. Same idea, C# shape.

## The `Validation<F, S>` type (verified, 4.4.9)

- **Shape:** `Success(S)` or **`Fail(Seq<F>)`** — failures accumulate in a **sequence** of `F`.
- **Construct:** `Success<F, S>(value)` / `Fail<F, S>(error)` (Prelude).
- **Combine (accumulate):** the tuple applicative — **`(v1, v2, …).Apply((a, b, …) => result)`**. All `Success` → `Success(result)`; any `Fail` → `Fail` with **all** collected errors.
- **Consume:** `.Match(Succ: s => …, Fail: (Seq<F> errs) => …)` — the `Fail` branch receives the whole `Seq<F>`.
- **It also has `Bind`** — which **short-circuits** (that's the contrast you'll prove in Step 6).
- **Convert:** `.ToEither()` → `Either<Seq<F>, S>`.

## Decisions made

- **Errors as `string` for the walkthrough** (`Validation<string, S>`) — keeps focus on accumulation; we'll note the upgrade to a domain error type as a stretch.
- **Domain:** validate the fields of an expense entry — `amount` (> 0), `category` (non-empty), `description` (non-empty). Multiple *independent* rules is what makes accumulation visible.
- *(record other choices as you go)*

---

## Candidate test list

- [ ] `ValidateAmount`: positive → `Success`; non-positive → `Fail`
- [ ] `ValidateCategory`: non-empty → `Success`; empty/whitespace → `Fail`
- [ ] **Accumulate:** amount *and* category both invalid → `Fail` with **both** messages
- [ ] one invalid → `Fail` with just that one
- [ ] all valid → `Success(entry)`
- [ ] `Match` consumes `Succ` / `Fail(Seq<F>)`
- [ ] **Contrast:** the same rules via `Bind` report only the *first* error (proves short-circuit vs accumulate)
- [ ] *(stretch)* a third field; a domain error type instead of `string`; `.ToEither()`

---

## Steps

### Step 1 — Red → Green: `ValidateAmount`  `[ ]`

Create `tests/Expenses.Tests/ExpenseValidationTests.cs`. Inline a validator with a deliberately-wrong stub (assertion red, not compile error):

```csharp
namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public static class ExpenseValidation
{
    // Deliberately wrong: always fails, so the first test reds on an assertion.
    public static Validation<string, decimal> ValidateAmount(decimal amount) =>
        Fail<string, decimal>("todo");
}

public class ExpenseValidationTests
{
    [Fact]
    public void ValidateAmount_Positive_ReturnsSuccess()
    {
        Validation<string, decimal> result = ExpenseValidation.ValidateAmount(12.50m);

        LangExtAssert.Equal(Success<string, decimal>(12.50m), result);
    }
}
```

Run — red. Then green:
```csharp
public static Validation<string, decimal> ValidateAmount(decimal amount) =>
    amount > 0
        ? Success<string, decimal>(amount)
        : Fail<string, decimal>("amount must be positive");
```
Note the explicit `<string, decimal>` type args on `Success`/`Fail` — there's no left/right value present for inference, so you supply both (same reason `Right<string, decimal>(…)` needed them in Phase 2).

### Step 2 — Triangulate + a second rule: `ValidateCategory`  `[ ]`

```csharp
[Fact]
public void ValidateAmount_NonPositive_ReturnsFail()
{
    var result = ExpenseValidation.ValidateAmount(-1m);
    Assert.True(result.IsFail);
}
```
Then add a second, independent rule (you'll need it to accumulate):
```csharp
public static Validation<string, string> ValidateCategory(string category) =>
    !string.IsNullOrWhiteSpace(category)
        ? Success<string, string>(category)
        : Fail<string, string>("category is required");
```

### Step 3 — Accumulate with `Apply` (the whole point)  `[ ]`

Add the result type and combine the two rules with the **tuple applicative**:
```csharp
public sealed record ValidatedEntry(decimal Amount, string Category);

public static Validation<string, ValidatedEntry> Validate(decimal amount, string category) =>
    (ValidateAmount(amount), ValidateCategory(category))
        .Apply((a, c) => new ValidatedEntry(a, c));
```

The star test — **both** fields invalid → **both** errors:
```csharp
[Fact]
public void Validate_AllFieldsInvalid_AccumulatesAllErrors()
{
    Validation<string, ValidatedEntry> result = ExpenseValidation.Validate(-5m, "");

    var errors = result.Match(Succ: _ => Seq<string>(), Fail: errs => errs);
    Assert.Equal(2, errors.Count);
    Assert.Contains("amount must be positive", errors);
    Assert.Contains("category is required", errors);
}
```
This is the payoff: unlike `Bind`, `Apply` ran **both** checks and gathered **both** failures. (`(v1, v2).Apply((a, b) => …)` is the tuple applicative — for more fields, extend the tuple: `(v1, v2, v3).Apply((a, b, c) => …)`.)

### Step 4 — The success and one-error paths  `[ ]`

```csharp
[Fact]
public void Validate_AllValid_ReturnsSuccessEntry()
{
    var result = ExpenseValidation.Validate(9.99m, "Groceries");
    LangExtAssert.Equal(Success<string, ValidatedEntry>(new ValidatedEntry(9.99m, "Groceries")), result);
}

[Fact]
public void Validate_OneFieldInvalid_FailsWithJustThatError()
{
    var result = ExpenseValidation.Validate(9.99m, "");     // only category bad
    var errors = result.Match(Succ: _ => Seq<string>(), Fail: e => e);
    Assert.Equal("category is required", errors.Single());
}
```

### Step 5 — Consume: `Match` on `Succ` / `Fail(Seq<F>)`  `[ ]`

```csharp
[Fact]
public void Match_RendersSuccessOrAllErrors()
{
    string ok  = ExpenseValidation.Validate(1m, "Food")
        .Match(Succ: e => $"OK: {e.Category}", Fail: errs => string.Join("; ", errs));
    string bad = ExpenseValidation.Validate(-1m, "")
        .Match(Succ: e => $"OK: {e.Category}", Fail: errs => string.Join("; ", errs));

    Assert.Equal("OK: Food", ok);
    Assert.Equal("amount must be positive; category is required", bad);
}
```
The `Fail` branch hands you the whole `Seq<F>` — join it, count it, render it, whatever the caller needs.

### Step 6 — Prove the contrast: `Bind` short-circuits  `[ ]`

This test *is* the lesson. Compose the same two rules with **`Bind`** instead of `Apply`, and watch only the first error survive:
```csharp
[Fact]
public void Bind_ShortCircuits_ReportingOnlyTheFirstError()
{
    // Bind: category check only runs if amount succeeded — so a bad amount hides the bad category
    Validation<string, ValidatedEntry> viaBind =
        ExpenseValidation.ValidateAmount(-5m)
            .Bind(a => ExpenseValidation.ValidateCategory("")
                           .Map(c => new ValidatedEntry(a, c)));

    var errors = viaBind.Match(Succ: _ => Seq<string>(), Fail: e => e);
    Assert.Equal("amount must be positive", errors.Single());   // ONLY the first — category never checked
}
```
Same inputs as Step 3 (both fields invalid), but `Bind` reports **one** error where `Apply` reported **two**. That difference is the monad-vs-applicative distinction made concrete: `Bind`'s second step *depends on* the first's success, so a failed amount means the category check never runs.

### Step 7 — Refactor: extract to production  `[ ]`

Green ⇒ promote. Move `ValidatedEntry` and `ExpenseValidation` into `src/Expenses` (namespace `Expenses`, `using LanguageExt; using static LanguageExt.Prelude;`). Delete the inline copies; add `using Expenses;`. Re-run the full suite (smoke + Phase 1/2 + kata all still green).

---

## Stretch (optional)

- **A third field** — add `ValidateDescription`, extend to `(v1, v2, v3).Apply((a, b, c) => …)`; confirm three errors accumulate.
- **A domain error type** — replace `string` with a `record ValidationError(...)` (or an enum), so `Validation<ValidationError, S>` carries structured failures. Feel when that beats strings.
- **`ToEither`** — `validation.ToEither()` → `Either<Seq<F>, S>`; note the bridge back to the short-circuiting world (and that the error side becomes the whole `Seq`).
- **Compose with Phase 2** — parse fields with `Fin`/`Either` (Phase 2), *then* validate the parsed values with `Validation`. Where does parsing (sequential) end and validation (parallel) begin?
- **LINQ preview (Phase 5)** — `Validation` also supports query syntax for the applicative; we'll formalize that next phase.

---

## Notes & questions

_Fill in as you go._

-
