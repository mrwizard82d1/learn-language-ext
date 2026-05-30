# Phase 0 — Setup

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `[ ]`. Flip to `[x]` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Expenses.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Stand up a fresh .NET 10 solution mirroring the structure of learn-xunit, with:

- `src/Expenses/` for production code (depends on LanguageExt)
- `tests/Expenses.Tests/` for xUnit v3 tests (transitively gets LanguageExt via project reference)
- A smoke test confirming xUnit AND LanguageExt are both wired up

## Decisions made

- **.NET 10 pinned via `global.json`.** Same approach as learn-xunit. Reproducible across machine reinstalls.
- **xUnit v3 with MTP.** Same testing toolchain as learn-xunit. Lifecycle, `[Theory]` patterns, serializer infrastructure — all carry over.
- **LanguageExt Core only.** `LanguageExt.Sys`, `LanguageExt.Pipes`, and other adjuncts aren't needed for this tutorial's scope. If we need more later, we'll add it explicitly.
- *(record the LanguageExt version you land on in Step 2 below — there's an active v4 → v5 transition and you want this pinned)*

---

## Steps

### Step 1 — Create the solution structure  `[ ]`

In `~/professional/projects/learn-language-ext/`, you want this layout:

```
learn-language-ext/
├── global.json
├── Expenses.sln
├── src/
│   └── Expenses/
│       └── Expenses.csproj
└── tests/
    └── Expenses.Tests/
        └── Expenses.Tests.csproj
```

Copy `global.json` directly from learn-xunit — same .NET 10 pin should work as-is.

CLI commands to run yourself when you're ready:

```
dotnet new sln -n Expenses
dotnet new classlib -n Expenses -o src/Expenses
dotnet new xunit3 -n Expenses.Tests -o tests/Expenses.Tests
dotnet sln add src/Expenses tests/Expenses.Tests
dotnet add tests/Expenses.Tests/Expenses.Tests.csproj reference src/Expenses/Expenses.csproj
```

After running these, verify both csproj files have `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`. The standard templates usually include both; double-check.

Delete the `Class1.cs` placeholder that `dotnet new classlib` generates in `src/Expenses/` — we'll start clean.

### Step 2 — Add the LanguageExt package  `[ ]`

```
dotnet add src/Expenses package LanguageExt.Core
```

Without a `--version` flag, NuGet resolves to the latest stable. As of 2026, LanguageExt v5 has been out for a while; the package should resolve to v5.x. If it resolves to v4.x, bump explicitly: `dotnet add src/Expenses package LanguageExt.Core --version 5.0.0` (adjust major.minor to whatever's current).

The v4 vs v5 distinction matters: APIs and namespace organization shifted between them. Older Stack Overflow answers and AI-generated examples are often v4-shaped; knowing your version saves debugging time later.

Once you know what landed, record the exact version in the **Decisions** section above.

The test project doesn't need a direct LanguageExt reference — transitive resolution from `src/Expenses` is sufficient. If we ever need LanguageExt types in test infrastructure that isn't going through production code, we'll add a direct reference at that point.

### Step 3 — Configure `xunit.runner.json`  `[ ]`

Copy `xunit.runner.json` from `learn-xunit/tests/Ledger.Tests/` to `tests/Expenses.Tests/` — same file, same settings.

Add to `Expenses.Tests.csproj` so it gets copied to output:

```xml
<ItemGroup>
  <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### Step 4 — Write the smoke test  `[ ]`

Create `tests/Expenses.Tests/SmokeTest.cs` with two `[Fact]`s, exercising both halves of the toolchain:

```csharp
namespace Expenses.Tests;

using LanguageExt;
using static LanguageExt.Prelude;

public class SmokeTest
{
    [Fact]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void LanguageExtIsAvailable()
    {
        Option<int> some = Some(42);
        Assert.True(some.IsSome);
    }
}
```

Two things you'll see again throughout the tutorial:

- **`using static LanguageExt.Prelude;`** brings `Some`, `None`, `Right`, `Left`, `Fin.Succ`, `Fin.Fail`, and a lot of other free functions into scope. This is the idiomatic LanguageExt usage — we'll lean into it from Phase 1 onward. (F# users will recognize the spirit: top-level constructor functions rather than nested static methods.)
- **`Option<int>`** is structurally what you already know from F#/Clojure/JS — the LanguageExt-specific idioms are mostly about syntax and interop, which Phase 1 covers.

### Step 5 — Verify  `[ ]`

From the project root:

```
dotnet test
```

Both smoke tests should pass.

**If `LanguageExtIsAvailable` fails to compile:** most likely v4-vs-v5 API drift on `Option`'s construction. Confirm the package resolved to v5 (Step 2). The `Some(...)` free function and `IsSome` property are both v5-stable.

**If test discovery doesn't find the tests:** compare your `Expenses.Tests.csproj` to `learn-xunit/tests/Ledger.Tests/Ledger.Tests.csproj` line-by-line. The MTP-specific properties (`<OutputType>Exe</OutputType>`, `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`, `<IsTestProject>true</IsTestProject>`) all need to be present.

### Step 6 — Capture decisions  `[ ]`

Update the **Decisions made** section with the actual LanguageExt version that landed. This is the project's record of "what was true when we started" — future-you will appreciate it.

---

## Stretch (optional)

- Skim the **LanguageExt v5 release notes** for the v4→v5 highlights. Knowing what changed will help you read older example code and tell which patterns are still current.
- Browse the [LanguageExt README](https://github.com/louthy/language-ext) for the current "what's in the box" — useful as you encounter types we don't cover in this tutorial.
- Open the LanguageExt source for `Option<T>` (look for `LanguageExt.Option`) and skim the public surface. With your F#/Clojure background, you'll recognize most of it; the few unfamiliar bits become Phase 1 talking points.

---

## Notes & questions

_Fill in as you go._

-
