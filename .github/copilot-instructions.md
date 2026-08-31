# Copilot instructions — Tnosc.EShop

Clean Architecture / DDD / CQRS eShop on `net10.0`, built on the in-repo `lib/` framework.
Full overview: [`AGENTS.md`](../AGENTS.md). Per-area policy: [`.github/instructions/`](./instructions).

## The eight things that break your first file

1. **Warnings are errors.** `TreatWarningsAsErrors` + `CodeAnalysisTreatWarningsAsErrors` +
   `AnalysisMode=All`, with Meziantou, Sonar, Roslynator and xunit analyzers on every project. A style
   nit fails the build as hard as a type error.
2. **`ImplicitUsings` is disabled.** Every `using` is explicit — including `System`, `System.Linq`,
   `System.Threading`.
3. **`CS1591` is a build error under `lib/`.** All five framework projects set
   `GenerateDocumentationFile=true`; write the XML doc, never suppress it.
4. **Central Package Management is on.** A `Version=` on a `PackageReference` is a build error — add a
   `<PackageVersion>` to `Directory.Packages.props` and reference the package bare.
5. **Every `.cs` file** opens with the TNOSC copyright header, then explicit `using`s (System first),
   then a **file-scoped** namespace. One public type per file, named after the file.
6. **Name every argument at every call site**, tests included. Explicit types over `var`. Primary
   constructors. More than two parameters ⇒ one per line, in the declaration *and* at the call site.
7. **Dependencies point inwards only.** Domain ← Application ← Api; Infrastructure implements. Domain
   takes no EF Core, ASP.NET or Npgsql. `Tests.Architecture` enforces this — a failure there is a
   design error, never something to suppress.
8. **Bounded contexts never reference each other** (`Catalog`, `Identity`, `Basket`, `Ordering`,
   `Payment`). They are folders inside each project, one Postgres schema each, plus `outbox`.
   Cross-context reaction happens through a domain event, in the *reacting* context's `EventHandlers/`.

## Definition of done

```bash
dotnet build Tnosc.EShop.slnx    # warnings are errors; clean build = done
dotnet test  Tnosc.EShop.slnx    # integration suite needs Docker running
```

A feature is not done until the build is clean, the new unit and integration tests are green, and the
architecture tests still pass. Report a skipped suite as skipped, never as passing.

## Where the detail lives

| Topic | Instructions file |
|---|---|
| File header, primary constructors, named arguments, `Async` naming, error codes | `code-style` |
| When a `#pragma` or `.editorconfig` entry is acceptable; what may never be suppressed | `analyzer-suppressions` |
| Central Package Management, justifying a new package, layer discipline for references | `dependencies` |
| `<Feature>Options` classes; `IConfiguration`/`IOptions<T>` touched exactly once | `configuration-options` |
| `[CacheTag]` values are constants in `Server.Shared`, never literals | `cache-tags` |
| `[DomainEventName]` as an immutable wire contract; the outbox and at-least-once delivery | `domain-events` |
| `[Idempotent]` claims its key in the handler's own transaction | `idempotency` |
| Two-context `dotnet ef` mechanics; reviewing the generated migration | `migrations` |
| Coarse roles in Keycloak, fine-grained permissions as constants in code | `authorization` |
| Blazor pages compose only; components own a colocated ViewModel + service | `blazor-client-mvvm` |
| Per-project conventions | `lib-framework`, `server-*`, `client-*`, `agent-stack`, `tests` |
