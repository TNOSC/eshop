---
name: ca-review
description: Review pending changes against Tnosc.EShop's Clean Architecture conventions — layer boundaries, rich-domain rules, no business branching, Result handling, CQRS split, decorators, outbox, and test coverage. Use when the user asks to review changes, check conventions, or audit a feature before committing.
argument-hint: [optional: files or feature to review; defaults to the working-tree diff]
---

# Convention Review

Review the given scope (default: `git diff` plus untracked files) against this repo's rules. Report
findings with `file:line`, ordered by severity. **Do not fix anything unless asked.**

Ground truth, in order: the scoped `CLAUDE.md` for each project touched, the root `CLAUDE.md`,
`# Clean Architecture Rules & Design.md`, and `plan/00-conventions.md`.

Much of this is already mechanised — **run `dotnet test tests/server/Tnosc.EShop.Server.Tests.Architecture`
first** and report failures as blockers. Then review what the tests cannot see.

## Checklist

### Layer boundaries — violations are blockers
- Domain references no EF Core, ASP.NET, Npgsql, or any outer layer.
- Application references no Infrastructure and no EF Core; no `DbContext` in a command handler.
- Api references no Infrastructure.
- No cross-context references (`Catalog` ↔ `Basket` ↔ …). Integration goes through domain events.
- Infrastructure carries no business control flow — it translates technical exceptions and nothing more.

### Rich domain
- Business rules live in the aggregate, a factory, a value object or a strategy — **not** in a handler,
  validator, endpoint, or EF configuration.
- No public setters; `private set`/`init` only; the only parameterless constructor is EF's private one.
- Every state-changing method calls `IncrementVersion()` — including `Create`.
- Value objects are sealed records with a private constructor and a `static Result<T> Create(...)`.
- Aggregate-spanning invariants (uniqueness) live in a `{Aggregate}Factory` taking the repository
  contract, not in the handler.
- Repository contracts live in **Domain**, next to their aggregate.

### Handlers
- **No business branching.** Only null checks, `Result`/error-state checks, cancellation checks.
  A `switch` on a domain enum, a threshold comparison, or a status test is a blocker.
- The handler never re-decides the domain's verdict — `Errors.ToArray()` is propagated unchanged,
  with the domain's `ErrorType` and code intact.
- Command handlers: `internal sealed`, repository contract + `IUnitOfWork`, call `SaveChangesAsync`
  themselves. Query handlers: `internal sealed`, in **Infrastructure.Persistence**, taking
  `EShopReadDbContext`, no repository reference, projecting a read model into a DTO.
- No god handlers: cross-cutting bloat → a decorator; workflow bloat → `I<X>Workflow` + step services.
- `[Transactional]` only for multi-aggregate work or a second `SaveChangesAsync` — not on a plain
  single-commit handler.

### Errors and results
- Expected failures return `Result`/`Result<T>`; exceptions are reserved for unexpected infrastructure
  failures and are normalised by `ExceptionDecorator`. No `try`/`catch` around business rules.
- Errors come from the aggregate's `{Aggregate}Errors` class, codes `{Aggregate}.{Reason}`, with the
  semantically correct `ErrorType` (it decides the HTTP status).
- Endpoints map only through `ToHttp` / `ToCreated` / `CustomResults.Problem` — never a hand-picked
  status for a failure, never a `try`/`catch`.

### Validation
- Validators are structural only (raw ids, DTO shape). Any format, length, range or uniqueness check
  duplicated from the domain is a finding — it will drift.
- A slice whose command carries no bare identifiers should have no validator at all.

### Persistence
- Table and column names are explicit `snake_case`; the context's `{Context}Schema` constants are used.
- No `HasConversion` for typed ids (`EntityIdConventions` handles them, foreign keys included).
- Read models are flat primitives implementing `IReadModel` — the write aggregate is never reused as one.
- Raw SQL only for multi-table joins, fully parameterised via `NpgsqlParameter`, with `Guid` (not typed
  ids) on the row type. Any interpolated value in SQL text is a blocker.
- Generated migrations reviewed: no unintended drop or rename, schema created before its tables.

### Caching and the outbox
- Every `[Cacheable]` query has matching `[CacheTag(...)]` on every command that mutates its data.
- **Cache tags are constants from `Server.Shared/<Context>/CacheTags.cs`.** Any string literal in a
  `[CacheTag(...)]` is a violation — the invalidating and populating handlers live in different
  projects, so a literal drifts without failing the build (`.claude/rules/cache-tags.md`).
- Domain events are raised by the aggregate (not the handler), carry flat primitives and ids, and have
  a unique `[DomainEventName("context.event-name.vN")]`.

### Style — build-breaking, so check them
- TNOSC copyright header, explicit `using`s (System first), file-scoped namespace, one public type per file.
- **Every argument named at every call site**; explicit types over `var`; braces always.
- XML docs on every public member in `lib/` (`CS1591` is an error there).
- No `Version=` on a `PackageReference` — Central Package Management.
- No new `#pragma` or `.editorconfig` suppression without a stated reason; ~60 rules are already suppressed.

### Tests
- New/changed command handlers: unit tests for the happy path, every propagated failure, and
  "does not commit on failure".
- New/changed domain code: one unit test per invariant, asserting the error code.
- New/changed query handlers: integration tests against real Postgres — not unit tests, never the
  in-memory provider.
- Integration tests seed through `UnitOfWork`, not `WriteContext`.
- Naming, `// Arrange`/`// Act`/`// Assert`, Shouldly over `Assert`.

## Output format

Group findings as **Blockers** (layer violations, business branching in a handler, interpolated SQL,
thrown exceptions for expected failures, architecture-test failures), **Convention violations**
(naming, placement, error codes, missing cache tags or domain events, duplicated validation), and
**Test gaps**. For each: `file:line`, what is wrong, and the one-line fix.

Close with a verdict — ready to commit, or what must change first. If everything passes, say so and
run `dotnet build Tnosc.EShop.slnx` + `dotnet test Tnosc.EShop.slnx` to confirm.

Report honestly: if a check could not be run (no Docker for the integration suite, for instance), say
which, rather than implying it passed.
