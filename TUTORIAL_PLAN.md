# Learn LanguageExt — Tutorial Plan

## Goal

Apply functional-programming discipline to C# using LanguageExt, focused on the types you'll actually reach for in everyday application code: `Option`, `Either`, `Fin`, `Validation`, persistent collections, and LINQ-style composition.

Scope explicitly **excludes** deep effect-system territory — `Eff<RT, A>`, `Aff<RT, A>`, free monads, HKT plumbing. These are interesting but rarely the right tool for typical application code, and they reshape a codebase in ways that don't pay off until the simpler patterns are internalized first.

By the end you should be able to:

- Replace nullable references with `Option<T>` fluently in LanguageExt's idiom (which differs in a few specifics from the F# / Clojure / JS `Option` patterns you already know).
- Express fallible operations with `Either<L, R>` / `Fin<T>` instead of exceptions.
- Accumulate multi-field validation errors with `Validation<F, S>` (vs short-circuiting with `Either`).
- Reach for LanguageExt's persistent collections naturally — calibrated to your existing Clojure / immutable-JS muscle memory.
- Compose multiple "effectful" operations with LINQ syntax cleanly.

## Setup notes

- **Project root**: `~/professional/projects/learn-language-ext/`
- **Editor**: JetBrains Rider via Gateway Remote Development (Path D, same as the learn-xunit project)
- **Runtime**: .NET 10 with `global.json` pin
- **Testing**: **xUnit 2 (classic VSTest)** — migrated from xUnit v3/MTP on 2026-07-10 to match Larry's team's stack and regain Rider's built-in test runner. Packages: `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `Microsoft.NET.Test.Sdk` 17.14.1.
- **Test runner**: Rider's **built-in runner** works (VSTest) — "Run All Tests from Solution" uses Rider's UI; `dotnet test` also works from the CLI. *(The earlier MTP "Test Explorer broken" caveat no longer applies.)*

## Phases

- [x] Phase 0 — Setup
- [x] Phase 1 — `Option<T>` the LanguageExt way
- [ ] Phase 2 — `Either<L, R>` and `Fin<T>`
- [ ] Phase 3 — `Validation<F, S>`
- [ ] Phase 4 — Persistent collections (LanguageExt flavor)
- [ ] Phase 5 — LINQ-style composition
- [ ] Phase 6 — Capstone

## Domain: expense tracker

Throughout, we'll build a small expense tracker. Each phase introduces a feature that exercises the LanguageExt type it's exploring, so the tracker grows naturally as you go:

- Parse expense entries (date, amount, category, description) — `Either`/`Fin` for parse failures
- Validate entries against multi-field rules — `Validation` for error accumulation
- Look up categories in a catalog — `Option` for misses
- Aggregate by category — `Map<K, V>`, `Lst<T>`/`Seq<T>` for grouped totals
- Generate a summary report — composition across all of the above

## Style

- **TDD red-green-refactor** as the default rhythm. Smoke tests are permanent canaries — kept, not deleted.
- **Compile errors are not valid "red" states.** Every red must be a runnable assertion failure.
- **New types start inline in test files.** Extract to production files as the first refactor after green.
- **You type the code.** I describe what to write and why; no shell commands or scaffolding files on your behalf unless you explicitly grant.
- **Candidate test lists** in phase docs (Kent Beck's pattern). Decisions captured in each phase doc's "Decisions" section as we make them.
- **Translation notes where useful**:
    - `NUnit ↔ xUnit` (continuing the testing translation from learn-xunit)
    - `F# / Clojure / immutable-JS ↔ LanguageExt` (leverage your FP background)

## Resume protocol

- Step headers end with `[ ]`. Flip to `[x]` when complete.
- First unchecked step is the resume point.
- "Notes & questions" at the bottom of each phase doc is yours — fill in as you go.

## Cross-project memory note

This project's Claude Code session will have its own memory directory (separate from learn-xunit's). When you first invoke Claude here, you may want to seed memory with the equivalents of:

- Your FP background (F#, Clojure, immutable-JS experience)
- Your TDD style and the "compile errors aren't reds" rule
- Your IDE preferences (Rider/IntelliJ keymap)
- Project setup (.NET 10, MTP runner, Path D)

Or re-tell me when relevant — your call. The fastest path is usually a single message at the start of a new session: "Same style as learn-xunit; FP background includes F#/Clojure/immutable-JS."
