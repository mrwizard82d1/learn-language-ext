# Phase 4 — Persistent collections (LanguageExt flavor)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `[ ]`. **You** flip them to `[x]` when complete — I'll just remind you.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** Rider's built-in test runner ("Run All Tests from Solution") or `dotnet test` (CLI).
- **TDD rhythm:** red → green → refactor. A red must be a *runnable assertion failure*, never a compile error. New types start **inline in the test file**; extracting to `src/Expenses` is the first refactor after green.
- **Assertions:** `LangExtAssert.Equal(expected, actual)` for any wrapper (`Option`/`Either`/`Fin`/`Validation`) comparison; plain `Assert.Equal` for collections/scalars.

## Goal

Aggregate expense data with **immutable, persistent collections** — build a category→total summary from a sequence of entries, and query it. The headline: LanguageExt's collections are **persistent** (every "mutation" returns a new value; the original is untouched), and **`Map.Find` returns `Option<V>`** — so lookups rejoin the Phase 1 world (present/absent), with no `null` and no `KeyNotFoundException`.

This is the phase where the earlier types compose with data structures: a `Seq<ExpenseEntry>` folds into a `Map<string, decimal>`, and each lookup hands you back an `Option`.

## What's actually different here (read this first)

You know persistent collections from **Clojure** (`assoc`/`update`/`merge-with`) and **immutable-JS** (`Map().set/.update`). LanguageExt is the same *idea*, C# shape — with a few specifics:

| You know | LanguageExt | Why it matters |
|---|---|---|
| Clojure/immutable-JS structural sharing | `Map`, `Seq`, `Lst`, `Set`, `HashMap`, `Arr` are all **persistent** — ops return a **new** collection, original unchanged | No defensive copying; "mutating" methods (`Add`, `SetItem`, `Remove`) are non-destructive. Confirmed below: `m1.Add(...)` leaves `m1` untouched. |
| Clojure's default map is **hashed/unordered**; JS `Map` is insertion-ordered | **`Map<K,V>` is *ordered*** (sorted by key, AVL tree). For hashed/unordered use **`HashMap<K,V>`** | `Map`'s `ToString`/enumeration comes out **sorted by key** — handy for a report, but it's *not* Clojure's default semantics. Pick `Map` (sorted) vs `HashMap` (hashed) deliberately. |
| `(get m k)` → `nil`; JS `map.get(k)` → `undefined` | **`map.Find(k)` → `Option<V>`** (`Some`/`None`) | Lookups return to Phase 1's world — no `null`, no `KeyNotFoundException`. The indexer `map[k]` still *throws* on miss; prefer `Find`. |
| Clojure `(update m k (fnil + 0) amt)` / `merge-with +` | **`map.AddOrUpdate(k, existing => existing + amt, () => amt)`** | The "add-or-accumulate" idiom — update the value if the key exists, insert if not. This is the heart of group-by-sum. |
| Clojure `reduce` / JS `reduce` | **`seq.Fold(seed, (acc, x) => …)`** | Left fold with an explicit seed — how you collapse a `Seq` into a `Map` of totals. |

The throughline: same persistent-collection instincts you already have, but **lookups are `Option`** and **updates are add-or-accumulate**.

## The collection types (verified, 4.4.9)

Confirmed in a scratch project against `LanguageExt.Core` 4.4.9:

- **Construct:** `Map(("a", 1), ("b", 2))`, empty `Map<string, int>()`, or `toMap(seqOfTuples)`. `ToString` → `[(a: 1), (b: 2)]` (**sorted by key**).
- **Lookup:** `m.Find("a")` → `Some(1)`; `m.Find("z")` → `None`. Indexer `m["b"]` → `2` (**throws** if absent). `m.ContainsKey("z")` → `false`.
- **Immutable add:** `var m2 = m1.Add("c", 3);` leaves `m1.Count` unchanged (`m2` is a new map).
- **Add-or-accumulate:** `m.AddOrUpdate(key, Some: existing => existing + n, None: () => seed)` — updates if present (`"a"` → `Some(101)`), inserts if absent (`"z"` → `Some(999)`).
- **Fold into a Map (group-by-sum):**
  ```csharp
  seqOfEntries.Fold(Map<string, decimal>(),
      (acc, e) => acc.AddOrUpdate(e.Category, cur => cur + e.Amount, () => e.Amount));
  // e.g. [("food",5),("food",3),("gas",10)] -> [(food: 8), (gas: 10)]
  ```
- **`.Keys` / `.Values`** → enumerables (wrap with `toSeq(...)` for a `Seq`). Also `.Count`, `.Remove(k)`, `.SetItem(k,v)`, LINQ, `.Map`, `.Filter`.
- **Siblings:** `Seq<A>` (you've used it — the `Validation` fail list), `Lst<A>` (immutable list), `Arr<A>` (immutable array), `Set<A>` (ordered set), `HashMap<K,V>`/`HashSet<A>` (hashed/unordered).

## Decisions made

- **Domain:** aggregate `ExpenseEntry` (from Phase 2's `src/Expenses`) by `Category` into a `Map<string, decimal>` of totals; then query it. Uses `Seq` (input), `Fold` + `AddOrUpdate` (aggregation), `Find` (query → `Option`).
- **Use `Map` (ordered)** for the totals — sorted-by-category output reads well in a report; note `HashMap` as the unordered alternative.
- *(record other choices as you go)*

---

## Candidate test list

- [x] `Map.Find` returns `Some(v)` for a present key, `None` for a missing key
- [x] `Add` is non-destructive: the original map is unchanged, the returned map has the new entry
- [x] `AddOrUpdate` updates an existing key and inserts a missing one
- [x] `SummariseByCategory`: a `Seq<ExpenseEntry>` folds to a `Map<string, decimal>` of per-category totals
- [x] empty input → empty `Map`
- [x] a category total is the **sum** of its entries (multiple entries accumulate)
- [ ] querying the summary: `TotalFor(category)` returns `Some(total)` / `None`
- [ ] *(stretch)* `HashMap` vs `Map` ordering; `Set` for distinct categories; `Keys`/`Values`; sort totals descending for a "top categories" view

---

## Steps

### Step 1 — `Map<K,V>` basics: `Find` → `Option`  `[x]`

Create `tests/Expenses.Tests/ExpenseSummaryTests.cs`. Start by *characterising* `Map` — a lookup returns `Option`, tying back to Phase 1:

```csharp
namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public class ExpenseSummaryTests
{
    [Fact]
    public void Map_Find_ReturnsSomeForHit_NoneForMiss()
    {
        var m = Map(("Food", 8m), ("Gas", 10m));

        LangExtAssert.Equal(Some(8m), m.Find("Food"));
        LangExtAssert.Equal(Option<decimal>.None, m.Find("Rent"));
    }
}
```

Notice: no `null`, no `KeyNotFoundException` — a miss is `None`, exactly Phase 1. (The indexer `m["Rent"]` *would* throw — prefer `Find`.)

### Step 2 — Persistence: `Add` is non-destructive  `[x]`

```csharp
[Fact]
public void Add_IsNonDestructive()
{
    var original = Map(("Food", 8m));
    var updated  = original.Add("Gas", 10m);

    Assert.Equal(1, original.Count);          // original untouched
    Assert.Equal(2, updated.Count);           // new map has both
    LangExtAssert.Equal(Option<decimal>.None, original.Find("Gas"));
}
```

This is the persistent-collection guarantee you know from Clojure/immutable-JS, in C#: the "mutation" returns a new value; the old one is unchanged (structural sharing under the hood).

### Step 3 — Add-or-accumulate: `AddOrUpdate`  `[x]`

The building block for grouping — update if the key exists, insert if it doesn't:

```csharp
[Fact]
public void AddOrUpdate_UpdatesExisting_InsertsMissing()
{
    var m = Map(("Food", 5m));

    var updated  = m.AddOrUpdate("Food", cur => cur + 3m, () => 3m);   // 5 -> 8
    var inserted = m.AddOrUpdate("Gas",  cur => cur + 3m, () => 3m);   // absent -> 3

    LangExtAssert.Equal(Some(8m), updated.Find("Food"));
    LangExtAssert.Equal(Some(3m), inserted.Find("Gas"));
}
```

`AddOrUpdate(key, Some: existing => …, None: () => …)` is LanguageExt's `merge-with`/`(update … (fnil + 0))`.

### Step 4 — The payoff: `SummariseByCategory` (fold a `Seq` into a `Map`)  `[x]`

Add the production function (inline for now). You'll need `ExpenseEntry` — reuse Phase 2's from `src/Expenses` (`using Expenses;`), or a small inline record if you prefer to keep this test self-contained:

```csharp
public static class ExpenseSummary
{
    public static Map<string, decimal> SummariseByCategory(Seq<ExpenseEntry> entries) =>
        entries.Fold(Map<string, decimal>(),
            (acc, e) => acc.AddOrUpdate(e.Category, cur => cur + e.Amount, () => e.Amount));
}
```

The star test — multiple entries per category **accumulate**:

```csharp
[Fact]
public void SummariseByCategory_SumsAmountsPerCategory()
{
    var entries = Seq(
        new ExpenseEntry(new DateOnly(2026, 1, 1), 5m,  "Food", "lunch"),
        new ExpenseEntry(new DateOnly(2026, 1, 2), 3m,  "Food", "snack"),
        new ExpenseEntry(new DateOnly(2026, 1, 3), 10m, "Gas",  "fill-up"));

    var totals = ExpenseSummary.SummariseByCategory(entries);

    LangExtAssert.Equal(Some(8m),  totals.Find("Food"));
    LangExtAssert.Equal(Some(10m), totals.Find("Gas"));
    Assert.Equal(2, totals.Count);
}
```

Then the empty case:

```csharp
[Fact]
public void SummariseByCategory_Empty_ReturnsEmptyMap()
{
    var totals = ExpenseSummary.SummariseByCategory(Seq<ExpenseEntry>());
    Assert.Equal(0, totals.Count);
}
```

*(Adjust the `ExpenseEntry` constructor call to match its actual shape in `src/Expenses`.)*

### Step 5 — Query the summary: `TotalFor` → `Option`  `[ ]`

Wrap the lookup so callers stay in the elevated world:

```csharp
public static Option<decimal> TotalFor(Map<string, decimal> summary, string category) =>
    summary.Find(category);

[Fact]
public void TotalFor_ReturnsSomeForKnown_NoneForUnknown()
{
    var totals = ExpenseSummary.SummariseByCategory(/* the Food/Gas seq */);

    LangExtAssert.Equal(Some(8m), ExpenseSummary.TotalFor(totals, "Food"));
    LangExtAssert.Equal(Option<decimal>.None, ExpenseSummary.TotalFor(totals, "Rent"));
}
```

The whole pipeline now speaks `Option` at the edges — a missing category is `None`, not an exception.

### Step 6 — Refactor: extract to production  `[ ]`

Green ⇒ promote `ExpenseSummary` into `src/Expenses` (namespace `Expenses`, `using LanguageExt; using static LanguageExt.Prelude;`). Delete the inline copy; re-run the full suite (smoke + Phases 1–3 + kata + coordinate + password all still green).

---

## Stretch (optional)

- **`Map` vs `HashMap`** — swap `Map` for `HashMap`; observe that `Map` enumerates **sorted by key** while `HashMap` doesn't. When does order matter?
- **Top categories** — turn the totals into a "biggest spend first" view: `toSeq(totals).OrderByDescending(kv => kv.Value)` (or LanguageExt ordering). Ordering is a *view* concern over the (sorted-by-key) `Map`.
- **`Set<string>`** — the set of distinct categories seen (`toSet(entries.Map(e => e.Category))`); union/intersect two months.
- **`Keys`/`Values`** — a grand total via `totals.Values.Sum()` or a `Fold`.
- **Compose with Phase 3** — validate entries with `Validation` first, then summarise only the `Success`es. Where does validation (per-entry) hand off to aggregation (across entries)?
- **LINQ preview (Phase 5)** — `from`/`group by`/`select` over the entries; contrast with the explicit `Fold`. (We'll formalise LINQ next phase — and per your preference, we'll keep the fluent form primary.)

---

## Notes & questions

_Fill in as you go._

-
