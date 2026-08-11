---
name: add-feature
description: Scaffold a complete Tnosc.EShop vertical slice — command or query, handler, validator, minimal-API endpoint, and unit + integration tests. Use when the user asks to add a feature, use case, command, query, or endpoint to this repo.
argument-hint: <feature description, e.g. "discontinue a product" or "list products by brand">
---

# Add a Feature (Vertical Slice)

Scaffold a full use case across Domain → Application → Infrastructure → Api → tests, following the
**Catalog** slice, which is the reference implementation. No MediatR: this codebase uses its own
`ICommand`/`IQuery` abstractions, Scrutor-registered handlers, and a decorator pipeline.

Read the root `CLAUDE.md` and the scoped `CLAUDE.md` in each project you touch before writing code.

## Workflow

1. **Classify.** A state change is a **command** (handler in `Server.Application`); a read is a
   **query** (handler in `Server.Infrastructure.Persistence`). This split is not negotiable — there
   is an architecture test for it.
2. **Check the Domain first.** The business rule belongs to an aggregate, factory, value object or
   strategy. If the aggregate, its `*Errors` entry, or a needed domain event is missing, add it
   first (use the `add-entity` skill). **If you find yourself about to write a business `if` in the
   handler, the rule is in the wrong layer.**
3. **Write the Application slice** — `Server.Application/<Context>/Commands/<Feature>/` or
   `…/Queries/<Feature>/`. See [references/command-slice.md](references/command-slice.md) and
   [references/query-slice.md](references/query-slice.md).
4. **Write the query handler in Infrastructure** (queries only) —
   `…Infrastructure.Persistence/<Context>/Queries/`, projecting a read model into the DTO.
5. **Write the endpoint** — `Server.Api/<Context>/<Feature>/`, plus a route constant in the
   context's `*Routes` class. See [references/endpoint.md](references/endpoint.md).
6. **Write tests** — unit tests for a command handler, integration tests for a query handler.
   See [references/tests.md](references/tests.md).
7. **Verify:** `dotnet build Tnosc.EShop.slnx` (warnings are errors), then
   `dotnet test Tnosc.EShop.slnx`. The architecture suite must stay green; the integration suite
   needs Docker running.

## Non-negotiable conventions

- **Every file** starts with the TNOSC copyright header, then explicit `using`s (System first;
  `ImplicitUsings` is off), then a file-scoped namespace.
- **Name every argument at every call site** — `Money.Create(amount: x, currency: y)`. Only `params`
  arrays are exempt.
- **Explicit types, not `var`**, except where the type is apparent (`new Product { … }`, `new()`).
- **No manual DI registration.** Command handlers, validators, domain-event handlers, query handlers,
  repositories and endpoints are all discovered by Scrutor/assembly scan. Never edit
  `ApplicationExtensions.cs` or `InfrastructurePersistenceExtensions.cs` for a new slice.
- **Handlers are `internal sealed`** with primary constructors, returning
  `ValueTask<Result<T>>` from `HandleAsync(command, cancellationToken = default)`.
- **No business branching in a handler.** Only null checks, `Result`/error-state checks and
  cancellation checks are permitted — a Roslyn architecture test fails the build otherwise.
- **Never re-decide the domain's verdict.** Propagate `result.Errors.ToArray()` unchanged; the
  `ErrorType` and code the domain chose are what the endpoint maps to HTTP.
- **Commands go through a repository contract + `IUnitOfWork`**, and call `SaveChangesAsync`
  themselves. Never inject a `DbContext` into a command handler.
- **Queries take `EShopReadDbContext`**, project a read model into a DTO, and never reference a
  repository or return a domain entity.
- **Validators do structural checks only** (raw ids, DTO shape). Formats, lengths, ranges and
  uniqueness belong to value objects, entities and factories — never duplicate them.
- **`[Transactional]` is opt-in**, only for multi-aggregate work or a second `SaveChangesAsync`.
  A single-aggregate, single-commit handler does not take it.
- **Cache attributes go on the handler class**: `[CacheTag("<context>")]` on commands that mutate
  cached data, `[Cacheable(seconds)]` on queries; `[CacheKey]` on query properties.
- Auth is **not wired yet** (Identity is task T11 in `PLAN.md`) — do not add `.RequireAuthorization()`
  or permission checks until it lands.

## Naming reference

| Artifact | Pattern | Lives in |
|---|---|---|
| Command | `{Verb}{Aggregate}Command` — `public sealed record : ICommand<T>` | `Server.Application/<Context>/Commands/<Feature>/` |
| Command handler | `{Command}Handler` — `internal sealed` | same folder |
| Validator | `{Command}Validator` — `internal sealed` | same folder |
| Query | `Get{X}Query` / `Search{X}Query` — `public sealed record : IQuery<T>` | `Server.Application/<Context>/Queries/<Feature>/` |
| DTO | `{X}Dto` — `public sealed record` | same folder as its query |
| Query handler | `{Query}Handler` — `internal sealed` | `…Infrastructure.Persistence/<Context>/Queries/` |
| Read model | `{Aggregate}ReadModel` — `internal sealed : IReadModel` | `…Persistence/<Context>/ReadModels/` |
| Endpoint | `{UseCase}Endpoint` — `internal sealed : IApiEndpoint` | `Server.Api/<Context>/<Feature>/` |
| Request | `{UseCase}Request` with a `ToCommand()` mapper | next to its endpoint |
| Error code | `{Aggregate}.{Reason}` | `Server.Domain/<Context>/<Aggregate>/{Aggregate}Errors.cs` |
| Test method | `HandleAsync_Should_{Outcome}_When_{Condition}` | `tests/server/…Tests.Unit` / `.Integration` |
