# Future Directions & Addenda

Forward-looking notes that don't belong to a single phase — things to revisit *after* the core tutorial, or deeper rabbit holes to descend deliberately rather than by accident.

---

## Functor / Applicative / Monad — and the LanguageExt v5 rewrite

**Larry's question (2026-07-31, verbatim):**

> I discovered that `language-ext` is undergoing / has undergone an extensive rewrite in version 5. Concepts like Monad, Applicative, and Functor now seem more critical to understanding the package and using it effectively. First, will the tutorial you designed cover these concepts in future lessons? Second, if not, how would you recommend that I learn enough about the concepts to understand their use in the implementation of `language-ext`? (Do I actually need this understanding?) I have a Master's degree in Physics (almost 40-years ago) so I'm not afraid of advanced math, but most of my education in mathematics has been in calculus and differential equations - not type theory.

### 1. Does the tutorial cover Functor / Applicative / Monad?

**Operationally, yes — as capabilities you *call*, not as named abstractions you *program against*.** This is a deliberate pedagogical choice (Wlaschin's "elevated world" style): name the *move*, not the algebra.

| Abstraction | The operation | Where in the tutorial |
|---|---|---|
| **Functor** | `Map` — transform the value inside, structure preserved | Phase 1 (`Option`) + Phase 2 (`Either`/`Fin`) — **done** |
| **Monad** | `Bind` (+ `pure`/return) — sequence *dependent* steps, short-circuit | Phase 2 `ParseEntry`, the library kata — **done** |
| **Applicative** | `Apply` (+ `pure`) — combine *independent* elevated values | **Phase 3 (`Validation`)** — `(v1, v2).Apply(...)` accumulating *both* errors *is* the applicative; it's the fix for the `ParseEntry_ShortCircuitsIncorrectlyIfBothBad` limitation |

So the three concepts are the *spine* of the tutorial — Functor, Applicative, Monad are exactly Wlaschin's "elevated world" trio. What the plan **explicitly scopes out** (`TUTORIAL_PLAN.md`) is *"deep effect-system territory — `Eff`, `Aff`, free monads, **HKT plumbing**."* That HKT plumbing is the piece that becomes prominent in v5.

### 2. Why v5 makes them *feel* more critical

- **v4 (what we're on — 4.4.9):** you use `Option`/`Either`/`Fin`/`Validation` as concrete types; the typeclass machinery is mostly hidden.
- **v5 (large rewrite):** built on higher-kinded **traits** (`Functor`, `Applicative`, `Monad`, `Traversable`, `Foldable`) with a higher-kinded-type encoding usually written `K<F, A>` ("some functor `F` holding an `A`"). These abstractions become **load-bearing in the type signatures** — you'll see `Monad<M>`, `K<F, A>` in signatures, error messages, and especially the reworked effect system (`IO`/`Eff`).

> ⚠️ **Verify before relying on v5 specifics.** v5 is a large, still-evolving rewrite that postdates reliable training knowledge. The concept-level description above is stable; the exact v5 API surface (trait names, the `K<>` encoding, migration steps) should be confirmed against Louth's current docs / blog before use.

### 2b. The v5 effect-system unification: one `IO<A>` instead of sync/async proliferation

**Larry's question (2026-07-31, verbatim):**

> I'm particularly intrigued by his decision to "extract" I/O concepts into their own package/library to avoid the proliferation of types (for example, Map, AsyncMap, and so on). Is this feature a v5 feature or is it present in the released 4.4.9?

**Answer (web-verified 2026-07-31 against primary sources):** it's a **v5 feature — not in 4.4.9.**

- **v4.4.9 (released, what this tutorial uses):** still has *separate* `Eff<A>` (sync) and `Aff<A>` (async) types, plus parallel `Option`/`OptionAsync`, `Either`/`EitherAsync` families. That sync/async split *is* the proliferation being eliminated.
- **v5 (still beta — e.g. `5.0.0-beta-77`; no stable `5.0.0` GA yet):** collapses all of it into a single **`IO<A>` monad** handling both sync and async, on the new higher-kinded traits (notably **`MonadIO`**) and monad transformers. `Aff` dropped; `*Async` variants dropped; `Eff` retained but rebuilt on top of `IO` (`Eff<A>` ≈ `Eff<Unit, A>`). Async families are replaced by transformers, e.g. `OptionAsync<A>` → `OptionT<IO, A>`.

**One correction to the framing:** it's the unification into their own **type/monad (`IO<A>`) inside `LanguageExt.Core`** — *not* a separate NuGet package. (`LanguageExt.Sys` is a separate package, but it already existed in v4 and isn't this.) So: a *type*, not a *package*.

**Motivation, in Louth's own words** — a "function colouring" argument:

> "Having to create an `*Async` variant for every type is an unreal amount of typing and opens up many opportunities for bugs." — [Discussion #1269 "Proposal: Drop all `Async` variants"](https://github.com/louthy/language-ext/discussions/1269)

> "Pretty much every `*Async` variant has been dropped. There is now just one type that does asynchronous code: `IO<A>`." … "the moment you use async — it colours your code in such a way that it makes it not compose with synchronous code and requires language features (rather than classic composition) to leverage." — [v5 FAQ](https://github.com/louthy/language-ext/wiki/Frequently-Asked-Questions)

The design move: make `IO<A>` the single home for async, use `fork` for parallelism, and *lift* `IO` into any monad transformer rather than pervasive `async`/`await`. This is downstream of the **same** higher-kinded-traits rewrite (§2) that makes Functor/Applicative/Monad visible in signatures — one rewrite, two faces.

Sources: [Discussion #1269](https://github.com/louthy/language-ext/discussions/1269), [Discussion #1393 (v4→v5 async changes)](https://github.com/louthy/language-ext/discussions/1393), [Discussion #1303 (5.0 alpha-1)](https://github.com/louthy/language-ext/discussions/1303), [v5 FAQ](https://github.com/louthy/language-ext/wiki/Frequently-Asked-Questions), [Higher Kinds blog](https://paullouth.com/higher-kinds-in-c-with-language-ext/), [NuGet LanguageExt.Core 4.4.9](https://www.nuget.org/packages/LanguageExt.Core/).

### 3. Do you actually need it? (tiered)

- **To *use* LanguageExt for application code (this tutorial's target): no type theory required.** The operational model — *stay in the elevated world; `Map` for plain results, `Bind` for dependent elevated results, `Apply` for independent ones; unwrap at the edge* — is the whole game. True in v4 **and** v5; the concrete types' surface (`.Map`/`.Bind`/`.Match`) barely changes.
- **To read v5's source, decode `K<F,A>` error messages, or write code generic over "any monad": yes — learn the *trait vocabulary*.** Modest amount of concept, not a type-theory course.
- **Deep category theory (natural transformations, adjunctions): no.** Never needed for practical use.

### 4. How to learn it — calibrated (physics background, not type theory)

The reassuring framing: **this is algebra, not type theory.** Functor/Monad/Applicative are defined by *laws* (identity, composition, associativity) — the same *shape* of reasoning as the algebraic structures met in physics (e.g. trusting operator linearity/associativity without re-deriving them). A calculus/DiffEq background is plenty; the missing piece is small.

Ranked path:

1. **Scott Wlaschin — "Elevated world" series** (fsharpforfunandprofit.com). "Understanding map and apply," "Understanding bind," "The Monad." Most accessible, C#/F#-flavored, zero category theory. **Likely start *and* stop here.**
2. **The laws, once**, as "why `Map`/`Bind` are predictable": Functor (identity + composition); Monad (left identity, right identity, associativity); Applicative (similar). ~10 minutes of "oh, that's all they are."
3. **Bartosz Milewski — "Category Theory for Programmers"** (free book + video series). The deep dive — genuinely enjoyable for a physicist, but *aspirational*, well beyond need. For curiosity, not prerequisite.
4. **v5-specific:** Paul Louth's blog + the LanguageExt wiki's higher-kinds material (verify current URLs).

**Intuition pump:** the elevated world is like a *change of representation* — moving into Fourier space, or a rotating frame. `Map` applies an operation *within* the representation without leaving it; `Bind` chains steps that each may re-enter it; the *laws* guarantee the bookkeeping stays consistent, the way you trust the algebra of operators to compose cleanly. Not rigorous, but the right shape.

### 5. v4 vs v5 for this tutorial — recommendation

**Lean: finish on v4.4.9.** It's stable, the operational concepts transfer 1:1 to v5, and v5's HKT visibility is a *distraction* while `Map`/`Bind`/`Apply` are still being cemented. Then treat v5 as a focused **"what changed and why"** follow-on once the fundamentals are reflexive. (Revisit if being on the current major version becomes a priority — then we'd plan a migration.)

### Open follow-ups (optional, when wanted)

- [~] **Research pass** to pin down current v5 specifics with live URLs: trait names, the `K<F, A>` encoding, Louth's migration guide. *(Partly done 2026-07-31 — §2b covers the `IO<A>` effect-system unification, `MonadIO`, transformers, and version boundary. Still open: the full `K<F,A>` HKT encoding details and a step-by-step migration guide.)*
- [ ] **Add a short "named abstractions" bridge** to the tutorial — one paragraph per phase (Phase 3 especially) naming the abstraction just used + its law, without derailing the operational focus.
- [ ] **v5 "what changed and why" mini-phase** as a post-tutorial capstone follow-on, if desired.

---

## Capstone (Phase 6) direction & architecture pre-work

**Decided direction (2026-07-31):** the capstone should implement **functional core / imperative shell**, optionally dressed in **Clean Architecture** layering — errors on the `Fin`/`Validation` rails, dependencies pushed to the edges ("dependency rejection"). Larry recalibrated *away* from leaning on the Contoso samples or MediatR after the research below; they're worth a *read*, not a *foundation*.

### The "Contoso" examples — de-emphasized (research 2026-07-31)

There are **two different Contoso examples**, neither authoritative:

1. **language-ext repo "Contoso University"** — community contribution by **Blake Saucier** (not Louth), merged 2019, pinned to **v3.3.28**. CQRS/**MediatR** web API using `Option`/`Either`/`Validation` + `Task` (pre-`Fin`/`Aff`/`Eff`/v5). **Removed from the current repo** — survives only in old history/forks; never ported. → historical curio only.
2. **Book *Practical functional C#*** by **Dimitrios Papadimitriou** (not Louth; likely the book Larry's reading — confirm exact title) — has its own "Contoso Clean Architecture with language-ext" chapter and an **MIT companion repo** (`dimitris-papadimitriou-chr/Practical-Functional-CSharp`, `WebApplicationExample`). Independent of #1.

**Three sources, three jobs** (if referenced at all): book's companion repo → *architecture shape* (read with sketch-first method, shape-not-syntax; ~v3/v4-era); repo Contoso University → *peek at MediatR/CQRS shape only*; current repo `Samples/` (`EffectsExamples`, `CardGame`, `DomainTypesExamples`) → *current v5 idioms* (but feature demos, not Clean-Arch apps).

### MediatR — study, don't depend (research 2026-07-31)

- **Licensing:** commercial move is real (launched **2025-07-02**, Lucky Penny Software, not walked back). **Free two ways** regardless: pin **`MediatR 12.5.0`** (last Apache-2.0; ≥13.0.0 is dual RPL-1.5/commercial), or use **13.x Community tier** (free under $5M revenue). So licensing is a non-blocker — but the baggage is a reason not to *build on* it.
- **FP critique:** routing through `IMediator` is effectively a **Service Locator** (hides dependencies), fighting the functional-core grain. Root idea: Seemann's **"dependency rejection"** — pure functions can't have dependencies, so an impure/pure/impure sandwich leaves nothing to mediate. *(Caveat: Seemann's canonical article doesn't name MediatR; the link is drawn by others, e.g. Arialdo Martini "You probably don't need MediatR.")* The one keeper concept is **pipeline behaviors**.
- **Functional analog of the mediator:** it collapses into **function composition**; pipeline behaviors → **higher-order functions decorating `Func<TIn, Fin<TOut>>`** (Seemann's "decorating functions"). Same cross-cutting benefit, no library, no license.
- **Verdict:** *study* MediatR (13.x Community — read a sample, grasp `IRequest`/handlers + pipeline behaviors); *build* the capstone with **direct handler injection or plain function composition returning `Fin`/`Validation`**, and a ~30-line dispatcher (or function decorators) if the pipeline ergonomics are wanted.

### Pre-work method (domain-agnostic, the durable part)

Regardless of what (if anything) gets read: before building, **sketch-first → read → revise**. Write a one-page architecture sketch of the capstone (layers, where parse/validate live, how "generate report" flows, where errors unwrap), *then* read a reference against it, *then* revise. The revised sketch becomes the capstone's design doc. Optional right-sized exercise: implement one use case **two ways** (handler/mediator-style vs. plain functional composition) and compare.

Key references: Seemann [dependency rejection](https://blog.ploeh.dk/2017/02/02/dependency-rejection/); Bernhardt "Boundaries" (functional core / imperative shell); the hexagonal/Clean-Architecture notes in `phases/phase-02-either-fin.md`.
