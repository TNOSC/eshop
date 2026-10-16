# Copilot instructions — Tnosc.EShop

This file is the repository-wide entry point for GitHub Copilot. Read the matching scoped policy in
[`instructions/`](./instructions/) before editing a project area; the rationale behind major decisions
lives in [`../docs/decisions/`](../docs/decisions/).

## Project map

| Editing | Instructions file |
|---|---|
| `lib/` | `lib-framework.instructions.md` |
| `…Server.Domain/` | `server-domain.instructions.md` |
| `…Server.Application/` | `server-application.instructions.md` |
| `…Server.Infrastructure.Persistence/` | `server-persistence.instructions.md` |
| `…Server.Api/` | `server-api.instructions.md` |
| `…Server.Shared/` | `server-shared.instructions.md` |
| `…Client.Web/` (BFF + host) | `client-bff.instructions.md` |
| `…Client.Web.Client/` (Blazor MVVM) | `client-blazor.instructions.md` |
| `…Client.Web.Contracts/` (shared DTOs) | `client-contracts.instructions.md` |
| `src/agent/` | `agent-stack.instructions.md` |
| `tests/` | `tests.instructions.md` |
| any `.cs` | `code-style`, `analyzer-suppressions`, `cache-tags`, `domain-events`, `idempotency`, `authorization`, `configuration-options` |

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

## Tech stack

| | Version | | Version |
|---|---|---|---|
| TFM / SDK | `net10.0` / .NET 10 preview (`10.0.400-preview`; no `global.json`) | Aspire | 13.5.2 (+ `Hosting.PostgreSQL`, `Hosting.Keycloak`, `Hosting.Foundry`, `Hosting.Redis`) |
| ASP.NET Core | 10.0.11 — Minimal APIs only, no MVC | Model Context Protocol | 2.2.0 |
| Keycloak | 26.6 — the only identity provider; owns users, credentials and realm roles | Microsoft Agent Framework | `Microsoft.Agents.AI` 1.18.0; hosting + AG-UI `1.18.0-preview.260818.1`; `Microsoft.Extensions.AI` 10.9.0 |
| EF Core | 10.0.11 (+ `.Relational`, `.Design`) | HybridCache | 10.8.0 |
| Npgsql EFCore | 10.0.3 — Postgres is the only database | OpenAPI | `AspNetCore.OpenApi` 10.0.11, `Scalar.AspNetCore` 2.10.0 |
| Fluent UI | `Microsoft.FluentUI.AspNetCore.Components` 5.0.0-rc.5-26219.1 | Azure AI / Foundry | `Azure.AI.Projects` 2.1.0-beta.4, `Azure.Identity` 1.21.0 |
| xUnit / Shouldly | 2.9.3 / 4.3.0 | OpenTelemetry | 1.17.0 (via `ServiceDefaults`) |
| NSubstitute / Bogus | 5.3.0 / 35.6.5 | Testcontainers / Respawn | 4.12.0 / 7.0.0 |
| NetArchTest / Roslyn | 1.3.2 / 5.0.0 | Analyzers | Meziantou, Sonar, Roslynator, xUnit |
| Scrutor | 7.0.0 (`Scan` + `TryDecorate`) | Aspire Keycloak | `Aspire.Hosting.Keycloak` + `Aspire.Keycloak.Authentication`, `13.5.2-preview.1.26421.6` |

## Structure and architecture

```text
lib/                     Reusable framework: Domain · Application · Api · Infrastructure.Persistence · Host
src/server/              Domain · Application · Infrastructure.{Persistence,External,Job} · Api · Shared · Host
aspire/                  AppHost (Postgres + pgAdmin + Keycloak resources) · ServiceDefaults (OTel, health, resilience)
tests/server/            Tests.{Unit,Integration,Architecture,Acceptance}
```

Dependencies point inwards only, enforced by `Tests.Architecture`:

| Layer | May contain | Must not reference |
|---|---|---|
| **Domain** | Entities, factories, strategies, VOs, domain events, repository contracts. Owns every business decision. | EF Core, ASP.NET, Npgsql, any outer layer |
| **Application** | Orchestration only: commands, handlers, validators, DTOs, workflows, ports | Infrastructure, EF Core |
| **Infrastructure** | Persistence, query handlers, EF config, gateways. Dumb and policy-free. | Business control flow |
| **Api** | Minimal-API endpoints, request contracts, `Result` → HTTP | Infrastructure |

Bounded contexts (`Catalog`, `Identity`, `Basket`, `Ordering`, `Payment`) are folders inside each
project and must not reference each other. Each has one Postgres schema, plus `outbox`; tables and
columns are `snake_case`, set explicitly in each `IEntityTypeConfiguration`. Catalog is the reference
implementation — copy its slice layout.

## Universal conventions

- **Configuration:** Settings go through a narrow, type-safe `<Feature>Options` class, not
  `IConfiguration` in a consumer's constructor. Configuration is bound and validated once in the owning
  `AddXxx` extension method.
- **Code style:** Follow the file header/layout, primary-constructor, named-argument, parameter-layout,
  `Async` naming, and error-code conventions in `code-style.instructions.md`.
- **Authorization:** Keycloak owns coarse realm roles (`admin`, `customer`); permissions are constants
  in `Server.Shared/Authorization/`. Endpoints use `.HasPermission(Permissions.X.Write)`, never a role
  or literal. `me` endpoints resolve callers from `IUserContext`; handlers do not check ownership.
- **Cache tags:** Use constants from `Server.Shared/<Context>/CacheTags.cs`, never string literals.
- **Domain events:** `[DomainEventName]` values are immutable wire contracts; publish through the outbox
  and design consumers for at-least-once delivery.
- **Idempotency:** `[Idempotent]` claims its key in the handler's own transaction.
- **Migrations:** Two EF contexts exist; always pass `--context`, and review generated migrations.

## Detailed policies

| Topic | Instruction file |
|---|---|
| File header, primary constructors, named arguments, `Async` naming, error codes | `code-style` |
| When a `#pragma` or `.editorconfig` entry is acceptable; what may never be suppressed | `analyzer-suppressions` |
| Central Package Management, package justification, layer discipline | `dependencies` |
| `<Feature>Options`; where configuration is bound and validated | `configuration-options` |
| `[CacheTag]` constants | `cache-tags` |
| `[DomainEventName]`, outbox and at-least-once delivery | `domain-events` |
| `[Idempotent]` transaction claim | `idempotency` |
| Two-context EF mechanics and migration review | `migrations` |
| Roles, permissions and authorization policy provider | `authorization` |
| Blazor pages, ViewModels, services and validation | `blazor-client-mvvm` |
| Per-project conventions | `lib-framework`, `server-*`, `client-*`, `agent-stack`, `tests` |

## Definition of done

```bash
dotnet build Tnosc.EShop.slnx    # warnings are errors; clean build = done
dotnet test  Tnosc.EShop.slnx    # integration suite needs Docker running
```

A feature is not done until the build is clean, the new unit and integration tests are green, and the
architecture tests still pass. Report a skipped suite as skipped, never as passing.

## Commands

```bash
dotnet test tests/server/Tnosc.EShop.Server.Tests.Unit --filter "FullyQualifiedName~ProductTests"
dotnet run --project aspire/Tnosc.EShop.AppHost         # Postgres + pgAdmin + Keycloak + host
dotnet run --project src/server/Tnosc.EShop.Server.Host # API alone; needs ConnectionStrings__eshopdb

# EF — always specify the write context; only it has migrations
dotnet ef migrations add <Name> --context EShopWriteDbContext \
  --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
  --startup-project src/server/Tnosc.EShop.Server.Host
```

The Aspire Postgres resource uses `WithDataVolume()`, so a schema change in development may require
dropping the volume first.

## Copilot configuration checks

```bash
copilot skill list          # every skill loads, no failures, no stray README entry
copilot mcp list            # aspire, fluent-ui-blazor
```

Inside `copilot`, `/instructions` lists discovered instruction files and lets you toggle them, `/agent`
lists custom agents, and `/env` shows instructions, MCP servers, skills, agents and hooks. Hooks have no
listing command; test one by piping a payload into it:

```bash
echo '{"toolName":"str_replace","toolArgs":{"path":"src/server/Foo.cs"}}' \
  | bash .github/hooks/posttooluse-csharp-conventions.sh
```

`preToolUse` command hooks fail closed: exit 2, a crash, an unbound variable or a syntax error denies
the tool call. Only a timeout fails open.