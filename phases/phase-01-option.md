# Phase 1 — `Option<T>` the LanguageExt way

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `[ ]`. **You** flip them to `[x]` when complete — I'll just remind you.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Expenses.Tests` Run configuration (Rider) or `dotnet test` (CLI).
- **TDD rhythm:** red → green → refactor. A red must be a *runnable assertion failure*, never a compile error. New types start **inline in the test file**; extracting to `src/Expenses` is the first refactor after green.

## Goal

Build the **category catalog** for the expense tracker: a lookup that returns `Option<Category>` — `Some` on a hit, `None` on a miss — and learn to *consume* that `Option` without ever unwrapping it unsafely.

You already know `Option`/`Maybe` cold from F#, Clojure, and immutable-JS. The point of this phase is **not** the concept — it's the handful of places LanguageExt's `Option<T>` differs from what your muscle memory expects, and the idiomatic ways to construct, transform, and consume it in C#.

## What's actually different here (read this first)

Translation notes, FP-background → LanguageExt:

| You know | LanguageExt `Option<T>` | Why it matters |
|---|---|---|
| F# `option` is a reference-type DU; can be `null` if you try hard | `Option<T>` is a **struct**. `default(Option<T>)` **is** `None`. It can *never* be null. | No `NullReferenceException` from an uninitialized Option. A field of type `Option<T>` is `None` before you assign it. |
| F# `Some null` is legal; Clojure/JS happily wrap nil/undefined | `Some(null)` **throws** `ValueIsNullException` at runtime. | LanguageExt enforces "Some means *definitely* a value." To wrap a possibly-null reference, use **`Optional(x)`** — null → `None`, non-null → `Some`. This is *the* bridge from C#'s nullable-reference world into `Option`. |
| F# `match`, Clojure `if-let`/`some->`, JS destructuring | `.Match(Some: x => …, None: () => …)`, `.IfNone(fallback)`, `.Map`, `.Bind`, `.Filter`, plus LINQ `Select`/`SelectMany`/`Where`. | There is deliberately **no safe `.Value` getter**. You get the value out by handling both cases. That's the discipline the type is enforcing. |
| F# `Option.map` / `Option.bind`; Clojure `some->`; JS `.map` on Maybe | `.Map` (functor) and `.Bind` (monad) — same laws, C# method names. `Map` over `None` is `None`; `Bind` chains lookups that each might miss. | Identical semantics to what you already use; only the names/syntax are new. |
| Equality of options in F#/Clojure is structural | `Option<T>` has **structural equality**. `Some(x) == Some(x)` when `x`s are equal; `None == None`. | Lets you assert `Assert.Equal(Some(expected), result)` directly. |

If something feels surprising later, it's almost certainly one of these five rows.

## Decisions made

- **The catalog is a plain `Dictionary<string, Category>` for this phase.** LanguageExt's own `Map<K,V>` has a `Find` that returns `Option<V>` natively — which is a lovely fit — but persistent collections are **Phase 4**. Keeping the backing store a BCL `Dictionary` here keeps the focus on `Option` itself, and gives us a concrete "interop with nullable BCL APIs" moment (that's where `Optional(...)` earns its keep). We'll revisit and likely swap to `Map` in Phase 4.
- *(record any other choices you make as you go)*

---

## Candidate test list

Kent Beck style — not all required, and order/scope is yours to adjust. Strike through as you land them.

- [x] Looking up a category that exists returns `Some(category)`
- [ ] Looking up a category that doesn't exist returns `None`
- [ ] `IfNone` supplies a fallback category on a miss
- [ ] `Match` runs the right branch for hit vs. miss
- [ ] `Map` transforms the found category (e.g. project its name) and is a no-op on `None`
- [ ] `Bind` chains a second lookup that itself returns `Option` (hit→hit, hit→miss, miss short-circuits)
- [ ] `Optional(null)` is `None`; `Optional(value)` is `Some` *(the nullable-bridge gotcha, pinned as a test)*
- [ ] *(stretch)* `Filter` rejects a category that fails a predicate, turning `Some`→`None`

---

## Steps

### Step 1 — Red: a known category is found  `[x]`

In `tests/Expenses.Tests/`, create `CategoryCatalogTests.cs`. Define the types **inline** for now, with a deliberately wrong stub so the test fails on an *assertion*, not on compilation:

```csharp
namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public sealed record Category(string Name);

public sealed class CategoryCatalog
{
    private readonly Dictionary<string, Category> _byName;

    public CategoryCatalog(IEnumerable<Category> categories) =>
        _byName = categories.ToDictionary(c => c.Name);

    // Deliberately wrong: always misses, so the first test goes red on an assertion.
    public Option<Category> Find(string name) => None;
}

public class CategoryCatalogTests
{
    [Fact]
    public void Find_KnownCategory_ReturnsSome()
    {
        var groceries = new Category("Groceries");
        var catalog = new CategoryCatalog([groceries]);

        Option<Category> result = catalog.Find("Groceries");

        Assert.Equal(Some(groceries), result);
    }
}
```

Run it. It compiles and **fails the assertion** (`Some(groceries)` vs `None`) — a valid red.

Note the assert style: because `Option<T>` has structural equality and `Category` is a record, `Assert.Equal(Some(groceries), result)` just works — no unwrapping. Also note `None` on the right of `=> None` converts cleanly to `Option<Category>` because that's the declared return type.

### Step 2 — Green: implement `Find`  `[x]`

Replace the stub body:

```csharp
public Option<Category> Find(string name) =>
    Optional(_byName.GetValueOrDefault(name));
```

Why this exact shape — it's the idiomatic LanguageExt move and worth dwelling on:

- `Dictionary.GetValueOrDefault(name)` returns `Category?` — the value, or `null` on a miss. This is the nullable BCL world.
- `Optional(x)` is the **bridge**: `null → None`, non-null → `Some(x)`. One function turns "nullable reference" into "honest `Option`." This is the single most useful interop function when consuming nullable-returning BCL APIs.
- A `TryGetValue` ternary — `_byName.TryGetValue(name, out var c) ? Some(c) : None` — also compiles and works here (C#'s target-typed conditional resolves both branches to `Option<Category>`). I'm steering you to the `Optional(...)` form anyway: it's a single expression, it states the intent ("turn this maybe-null into an Option") directly, and it's the pattern you'll reuse at every nullable boundary. *(Verified against 4.4.9: both forms compile — there's no "no common type" trap in this stack, despite what older LanguageExt/C# write-ups claim.)*

Run — green.

### Step 3 — Triangulate: a missing category returns `None`  `[x]`

```csharp
[Fact]
public void Find_UnknownCategory_ReturnsNone()
{
    var catalog = new CategoryCatalog([new Category("Groceries")]);

    Option<Category> result = catalog.Find("Rent");

    Assert.True(result.IsNone);
}
```

This likely passes **immediately** against the Step 2 implementation. That's expected and fine — triangulation here is confirming the behavior, not driving new code. (If it had failed, you'd have a bug in `Find`.)

### Step 4 — Consume without unwrapping: `IfNone` and `Match`  `[x]`

The whole point of `Option` is handling both cases at the *use* site. Two tests:

```csharp
[Fact]
public void IfNone_OnMiss_SuppliesFallbacki()
{
    var catalog = new CategoryCatalog([new Category("Groceries")]);
    var uncategorized = new Category("Uncategorized");

    Category result = catalog.Find("Rent").IfNone(uncategorized);

    Assert.Equal(uncategorized, result);
}

[Fact]
public void Match_RunsTheCorrectBranch()
{
    var catalog = new CategoryCatalog([new Category("Groceries")]);

    string label = catalog.Find("Groceries")
        .Match(
            Some: c => $"Found: {c.Name}",
            None: () => "Not found");

    Assert.Equal("Found: Groceries", label);
}
```

`IfNone` is your `Option.defaultValue` (F#) / `(get m k default)` (Clojure) / `?? fallback` (C#). `Match` is the explicit two-branch fold — the C# stand-in for F#'s `match … with`. Note `Match` here *returns* a value (an expression); there's also a statement form with `Action` branches when you want side effects.

### Step 5 — Transform and chain: `Map` and `Bind`  `[ ]`

> **Reweighted on purpose.** You already know `Map` cold, so it gets a 30-second confirmation. `Bind` is the focus — it's the one that's less familiar coming from a Map-heavy background, and it's the engine behind Phase 5's LINQ syntax. Tests use the constructor-hoisted `_catalog` from your Step-4 refactor.

#### 5a — `Map` (quick confirmation)  `[x]`

Functor map: `A → B`, applied if `Some`, no-op on `None`. Nothing new conceptually — one test to pin the LanguageExt syntax, then move on.

```csharp
[Fact]
public void Map_ProjectsName_AndIsNoOpOnMiss()
{
    Option<string> hit  = _catalog.Find("Groceries").Map(c => c.Name);
    Option<string> miss = _catalog.Find("Rent").Map(c => c.Name);

    Assert.Equal(Some("Groceries"), hit);
    Assert.True(miss.IsNone);
}
```

#### 5b — `Bind` (the focus)  `[ ]`

First, a second `Option`-returning step to chain — a multi-level category rollup, so we can build chains more than one link long. Add to `CategoryCatalog` (inline for now):

```csharp
public Option<Category> FindParent(Category c) =>
    c.Name switch
    {
        "Groceries" => Some(new Category("Food")),
        "Food"      => Some(new Category("All Spending")),
        _           => None
    };
```

**Beat 1 — feel the nesting pain.** `FindParent` returns `Option<Category>`. Watch what happens if you `Map` it (a function that *itself* returns an `Option`) versus `Bind` it:

```csharp
[Fact]
public void Map_OfAnOptionReturningFn_Nests_WhereBindFlattens()
{
    // Map wraps the function's (already-Option) return in ANOTHER Option:
    Option<Option<Category>> nested = _catalog.Find("Groceries").Map(_catalog.FindParent);
    Assert.Equal(Some(Some(new Category("Food"))), nested);   // Option<Option<…>> — nested!

    // Bind flattens it — Map + flatten:
    Option<Category> flat = _catalog.Find("Groceries").Bind(_catalog.FindParent);
    Assert.Equal(Some(new Category("Food")), flat);            // Option<Category> — flat
}
```

That `Option<Option<Category>>` is the whole "aha": **`Bind` = `Map` + flatten.** (You may know `Bind` under other names: `flatMap`, `SelectMany`, `>>=`.)

**The decision rule — make it a reflex:**

| Your function's shape | Use |
|---|---|
| `A → B` (returns a *plain* value) | `.Map` |
| `A → Option<B>` (returns a *wrapped* value) | `.Bind` |

"Does the step I'm chaining *itself* have an absence/failure outcome? → `Bind`."

**Beat 2 — the railway: short-circuit down a multi-link chain.** Any `None` anywhere collapses the rest, with no further work:

```csharp
[Fact]
public void Bind_ChainsLookups_AndShortCircuits()
{
    // hit → hit → hit : the happy path runs end to end
    Option<Category> twoUp =
        _catalog.Find("Groceries").Bind(_catalog.FindParent).Bind(_catalog.FindParent);
    Assert.Equal(Some(new Category("All Spending")), twoUp);

    // miss at the FIRST step → the rest is skipped entirely
    Option<Category> missChain =
        _catalog.Find("Rent").Bind(_catalog.FindParent).Bind(_catalog.FindParent);
    Assert.True(missChain.IsNone);

    // hit → hit → miss ("All Spending" has no parent) → None
    Option<Category> hitThenMiss =
        _catalog.Find("Groceries").Bind(_catalog.FindParent).Bind(_catalog.FindParent).Bind(_catalog.FindParent);
    Assert.True(hitThenMiss.IsNone);
}
```

You write the happy path linearly; the `None` case is handled for free at every link. *That* short-circuiting is the payoff.

**Beat 3 — you already know `Bind`, under other names.** Anchor it to your background:

- **Clojure `some->`** — threads a value through forms, bailing on the first `nil`. That's `Bind`-chaining. You've used `Bind` for years.
- **JS `Promise.then`** — flattens nested promises instead of handing you `Promise<Promise<T>>`. Monadic bind for `Promise`.
- **F# `Option.bind`** — literally this.
- **LINQ `SelectMany`** — *is* `Bind`. This sets up **Phase 5**: `from x in … from y in … select …` desugars to `SelectMany`/`Bind`. Seeing the explicit `.Bind` now makes that query syntax read as pure sugar.

So you're not really *learning* `Bind` — you're recognizing the `some->` / `.then` flattening you already rely on, now spelled `Bind` and made type-safe.

### Step 6 — Refactor: extract to production code  `[ ]`

Now that it's green, do the first refactor: move `Category` and `CategoryCatalog` out of the test file into `src/Expenses`.

- `src/Expenses/Category.cs` — the `record`.
- `src/Expenses/CategoryCatalog.cs` — the class.
- Both under `namespace Expenses;`.
- Delete the inline copies from the test file; add `using Expenses;` to the test file.
- Re-run the full suite (smoke tests included — they're permanent canaries). Still green = clean refactor.

This is the rhythm for every phase: prove behavior with inline types under test, then lift them into production once green.

### Step 7 — Pin the nullable-bridge gotcha  `[ ]` *(short)*

Lock in the `Optional` semantics with a tiny direct test, so the "why not `Some`?" lesson is documented in code:

```csharp
[Fact]
public void Optional_TreatsNullAsNone()
{
    string? missing = null;
    string present = "x";

    Assert.True(Optional(missing).IsNone);
    Assert.Equal(Some("x"), Optional(present));
}
```

---

## Stretch (optional)

- **`Filter`**: `catalog.Find("Groceries").Filter(c => c.Name.Length > 100)` turns `Some`→`None` when the predicate fails. Same as `Option.filter` (F#) / `Where` on a Maybe. Add a test.
- **Pattern matching interop**: explore `result.Case` — in v4 it lets you `switch` on the `Some`/`None` shape with C# pattern syntax, as an alternative to `.Match`.
- **`ToNullable` / `IEnumerable`**: `Option<T>` interops both ways — `.ToNullable()` (for value types) and it's enumerable (0 or 1 elements). Useful at the boundary with non-LanguageExt code.
- **Source skim**: open `LanguageExt.Option<A>` (4.4.9) and scan the public surface — with your background most of it reads itself; flag anything unfamiliar as a question below.

---

## Notes & questions

A key idea from step 2, `Optional<T>` is critical bridge type between the BCL world full or `null` values and the functional, or more accurately, Option, world of `LanguageExt`.

### The BCL ↔ functional bridge (two families, verified against 4.4.9)

Think of it as a conceptual two-way interface. **Lifts** get you *into* the Option world; **eliminators** get you back *out*.

**BCL → `Option` (lifts — total & safe; null/empty/parse-failure all collapse to `None`):**

| BCL absence idiom | Lift |
|---|---|
| nullable ref / `Nullable<T>` | `Optional(x)` |
| empty sequence | `xs.HeadOrNone()` |
| `TryParse` out-param dance | `parseInt(s)`, `parseDouble`, `parseGuid`, `parseBool`, … |
| dictionary miss | `Optional(dict.GetValueOrDefault(k))` |

**`Option` → BCL (eliminators — you must say what absence *becomes*):**

| Target | Eliminator |
|---|---|
| nullable value type `T?` (`where T : struct`) | `.ToNullable()` |
| nullable reference | `.IfNoneUnsafe(null)` / `.MatchUnsafe(Some:…, None:…)` |
| a guaranteed value (safe exit) | `.IfNone(fallback)` / `.Match(Some:…, None:…)` |
| 0-or-1 collection | `.ToSeq()` / `.ToList()` / `.AsEnumerable()` |

**The asymmetry to internalize:** lifting is frictionless; lowering forces a decision. Any eliminator that can **reintroduce `null`** is suffixed **`Unsafe`** (`IfNoneUnsafe`, `MatchUnsafe`) — the type system flagging that you're stepping back out of the safe world. There is deliberately **no `.Value`** getter. This same lift/eliminate shape recurs for `Either`, `Fin`, and `Validation` in later phases.

### Vocabulary cheat-sheet (new terms)

The big idea: every algebraic type comes as a **matched pair** — ways to *build* a value (introduction) and ways to *use* one (elimination). This duality is from Gentzen's natural-deduction logic (1930s) → Martin-Löf type theory → FP.

| Term | Synonyms | What it means | `Option` examples |
|---|---|---|---|
| **Constructor** | introducer, introduction rule, intro form | builds a value of the type ("way in") | `Some`, `None`, `Optional` |
| **Eliminator** | destructor, fold, catamorphism, case analysis, consumer, observer | consumes/takes apart a value ("way out") | `Match`, `IfNone`, `ToNullable`, `ToSeq` |
| **Lift (value)** | `pure`, `return`, `unit` | put a plain `A` into a wrapper: `A → Option<A>` | `Some(x)` / `Optional(x)` |
| **Lift (function)** | functor `map` / `fmap` | make a plain function work on wrapped values: `(A → B) → (Option<A> → Option<B>)` | `.Map(f)` |

Notes to self:
- **"Destructor" here ≠ C++ destructor.** It just means "a function that consumes/deconstructs a value." (C#'s `Deconstruct` methods are a nod to this.)
- **`Match` is the fold/catamorphism for `Option`** — the canonical eliminator; everything else (`IfNone`, `ToNullable`, …) can be defined in terms of it.
- **The true dual of *eliminator* is *constructor***, not "lift." I used "lift" loosely in the bridge table above; precisely, the in/out duality is **introduction ↔ elimination**, and "lift" is the related idea of moving a value or function *into* a wrapped context.
- Same `bool` you've used forever fits the frame: its constructors are `true`/`false`, its eliminator is `if`/pattern-match.

### Duality (and why Rx is "the dual of LINQ")

**Duality** is a formal idea from **category theory** (the source of most FP vocabulary), not just a metaphor. Model things as *objects* + *arrows* (morphisms); the **dual** of any construction is what you get by **reversing all the arrows** (working in the opposite category `Cᵒᵖ`). Consequences:

- Every concept gets a "co-" partner: product / **co**product, monad / **co**monad, algebra / **co**algebra, limit / **co**limit. The `co-` prefix literally means "the dual of."
- Theorems come in pairs for free — prove it in `C`, the arrow-reversed version holds in `Cᵒᵖ`.

**Rx really is the categorical dual of LINQ** (Erik Meijer's derivation): reverse the arrows of the *pull* iterator `IEnumerable`/`IEnumerator` and you get the *push* observer `IObservable`/`IObserver`, with `MoveNext → OnNext`, done `→ OnCompleted`, threw `→ OnError`. Not analogy — derivation.

**Ties back to constructor/eliminator:**
- **Data** — defined by *constructors*, consumed by *folds* (catamorphisms). You build it up. `List`/`IEnumerable` live here; `Option`'s `Match` is on this side.
- **Codata** — defined by *destructors/observations*, produced by *unfolds* (anamorphisms). You observe it, maybe forever. Streams/`IObservable` live here.
- Data ↔ codata are duals (initial algebra vs. final coalgebra) — the same arrow-reversal that turns `IEnumerable` into `IObservable`.

Logic angle (the natural-deduction roots): introduction ↔ elimination, ∧ ↔ ∨ (De Morgan), ∀ ↔ ∃ are all dual pairs. Same idea, different category.

### `IfNone` vs `IfSome` — a *deliberate* asymmetry (verified, 4.4.9)

They look like a symmetric pair but aren't, and the asymmetry is principled:

| | Signature | Returns | Purpose |
|---|---|---|---|
| `IfNone` | `IfNone(A fallback)` / `IfNone(Func<A>)` | **`A`** (a value) | *Extract* — give me the value, or this fallback. Safe eliminator to a bare value. |
| `IfSome` | `IfSome(Action<A>)` | **`Unit`** | *Side effect* — run this action **only if** `Some`. |

- Value-extraction has exactly **one** natural home: `IfNone`. The fallback parameter belongs on the `None` side because that's the case where you must supply something. `"give me the value if it's None, otherwise…"` has no coherent meaning — so there's no value-returning `IfSome`.
- `IfSome` is for **effects** when a value is present; it returns `Unit` (LanguageExt's real "no meaningful value", unlike `void`).
- Want a *symmetric* both-branches construct? That's **`Match`** — both branches return the same type. `IfNone`/`IfSome` are the asymmetric one-sided conveniences; `Match` is the balanced fold.

**`Match` argument order:** with **named** args (`Some:`/`None:`) order is irrelevant — the name binds the branch. Only the **positional** overload is order-sensitive (convention: `Some` first, `None` second). Prefer named.

**`IfNone` has two overloads — eager value vs lazy thunk:**

```csharp
A IfNone(A noneValue)         // eager: fallback already computed (normal C# arg evaluation)
A IfNone(Func<A> noneFactory) // lazy: fallback computed ONLY on the None path
```

- The eager arg is evaluated *before* `IfNone` runs — so a costly default is paid on every call, hit or miss.
- The `Func<A>` form defers it: invoked only down the `None` branch. Use it when the fallback is expensive / allocates / has side effects (DB, IO, logging). Cheap constant → use the value form.
- Same eager-vs-thunk pairing recurs across LanguageExt; cf. F# `defaultValue` vs `defaultWith`, Clojure `(or x (expensive))`.

### xUnit shared setup vs NUnit (translation note)

**xUnit has NO `[SetUp]`/`[TearDown]`.** The core rule: a **fresh instance of the test class is constructed for every test method** — so "setup" is the **constructor**, "teardown" is `Dispose()` (`IDisposable`) or `IAsyncLifetime` for async. That per-test-instance rule is what makes isolation automatic.

Three tiers, mapped to pytest `scope=`:

| Need | xUnit | NUnit | pytest |
|---|---|---|---|
| Fresh setup per test (default) | **constructor** (+ `Dispose`) | `[SetUp]`/`[TearDown]` | `scope="function"` |
| One shared instance per **test class** | **`IClassFixture<T>`** (injected via ctor param) | `[OneTimeSetUp]` | `scope="class"` |
| One shared instance across **many classes** | **`ICollectionFixture<T>`** | — | `scope="module"`/`"session"` |

- `IClassFixture`/`ICollectionFixture` are the true "inject a fixture object" model — xUnit injects them as **constructor parameters**.
- **Trade-off:** constructor = isolation (fresh per test); class/collection fixtures = sharing (one instance). Sharing is only safe when the fixture is **immutable/read-only**, else tests leak state into each other. Choose by cost vs. isolation, like a pytest scope.
- Applied here: the `Groceries` catalog is cheap + immutable, so hoisted into the **constructor** (`_catalog`). Each `[Fact]` gets its own instance; per-test locals (`uncategorized`, `factoryCalls`, `seen`) stayed local.

-
