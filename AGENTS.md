# AGENTS.md

Clean Architecture / DDD / CQRS eShop built on the in-repo `lib/` framework.

**Read the instructions file for the tree you are editing.** The narrow, per-area policies live in
[`.github/instructions/`](./.github/instructions) as `*.instructions.md` files carrying an `applyTo`
glob — your assistant loads the matching ones automatically when it touches a file in that tree. The
reasoning behind the decisions is in [`docs/decisions/`](./docs/decisions).

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

## Which file is authoritative

This repo is configured for two assistants, and the split is deliberate:

- **The ten narrow policies** (code style, cache tags, idempotency, migrations, domain events,
  dependencies, analyzer suppressions, configuration options, authorization, Blazor MVVM) live **only**
  in `.github/instructions/`. The files under `.claude/rules/` are one-line stubs pointing here — edit
  the instructions file, never the stub.
- **The per-project conventions** are duplicated: the body lives in both the scoped `CLAUDE.md` and
  the matching `.github/instructions/*.instructions.md`, because Claude Code reads the former and
  Copilot reads the latter. **Change one and you must change the other.**
- This file and `CLAUDE.md` hold the same repo-wide overview, for the same reason.


## Tech stack

| | Version | | Version |
|---|---|---|---|
| TFM / SDK | `net10.0` / 10.0.400-preview (no `global.json`) | Aspire | 13.5.0 (+ Hosting.PostgreSQL, Npgsql.EFCore) |
| ASP.NET Core | 10.0.10 — Minimal APIs only, no MVC | Scrutor | 7.0.0 (`Scan` + `TryDecorate`) |
| Keycloak | 26.6 — the only identity provider; owns users, credentials and realm roles | Aspire Keycloak | `Hosting.Keycloak` + `Keycloak.Authentication`, `13.5.0-preview.1.26417.10` (no stable build exists) |
| EF Core | 10.0.10 (+ `.Relational`, `.Design`) | HybridCache | 10.8.0 |
| Npgsql EFCore | 10.0.3 — Postgres is the only database | OpenAPI | AspNetCore.OpenApi 10.0.10, Scalar 2.10.0 |
| xUnit / Shouldly | 2.9.3 / 4.3.0 | OpenTelemetry | 1.17.0 (via `ServiceDefaults`) |
| NSubstitute / Bogus | 5.3.0 / 35.6.5 | Testcontainers / Respawn | 4.12.0 / 7.0.0 |
| NetArchTest / Roslyn | 1.3.2 / 5.0.0 | Analyzers | Meziantou, Sonar, Roslynator, xunit |

**Central Package Management is on** — never put `Version=` on a `PackageReference`; add a `<PackageVersion>` to `Directory.Packages.props` and reference the package bare.

## Build constraints (they bite in the first file you write)

`Directory.Build.props` applies everywhere: `TreatWarningsAsErrors` ·
`CodeAnalysisTreatWarningsAsErrors` · `AnalysisMode=All` · `AnalysisLevel=latest` ·
`EnforceCodeStyleInBuild` · `Nullable=enable` · `ImplicitUsings=disable`.

- **Every `using` is explicit** — including `System`, `System.Linq`, `System.Threading`.
- All five `lib/` projects set `GenerateDocumentationFile=true` ⇒ `CS1591` is a build **error**.
- ~60 analyzer rules are already suppressed in `.editorconfig` (`CA1707`, `CA1515`, `CA2007`,
  `CA1031`, `CA1034`, `CA1812`, `MA0004`, …). Check there before reaching for a `#pragma`.

## Structure

```
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
| **Infrastructure** | Persistence, query handlers, EF config, gateways. "Dumb and policy-free." | — (no business control flow) |
| **Api** | Minimal-API endpoints, request contracts, `Result` → HTTP | Infrastructure |

Bounded contexts (`Catalog`, `Identity`, `Basket`, `Ordering`, `Payment`) are folders inside each
project and **must not reference each other**. One Postgres schema per context, plus `outbox`;
tables and columns are `snake_case`, set explicitly in each `IEntityTypeConfiguration`.
Catalog is the reference implementation — copy its slice layout.

## Universal conventions

**Configuration and options:** See [`configuration-options.instructions.md`](./.github/instructions/configuration-options.instructions.md) — any
settings read from `appsettings.json` go through a narrow, type-safe `<Feature>Options` class, never
`IConfiguration` in a consumer's constructor. `IConfiguration` is touched exactly once, in the owning
`AddXxx` extension method, to bind and validate.

**Code style:** See [`code-style.instructions.md`](./.github/instructions/code-style.instructions.md) — file header and
layout, primary constructors, named arguments, one parameter per line past two, `Async` naming, and
error-code/`ErrorType` conventions.

**Authorization:** See [`authorization.instructions.md`](./.github/instructions/authorization.instructions.md) — Keycloak
owns the coarse realm roles (`admin`, `customer`) and who holds them; this codebase owns the
permission vocabulary as constants in `Server.Shared/Authorization/` and the role → permission map.
Endpoints name a permission via `.HasPermission(Permissions.X.Write)`, never a role and never a
literal. `me` endpoints resolve the caller from `IUserContext`, so no handler contains an ownership
check.

## Commands

```bash
dotnet build Tnosc.EShop.slnx                          # warnings are errors; clean build = done
dotnet test  Tnosc.EShop.slnx                          # integration suite needs Docker running
dotnet test  tests/server/Tnosc.EShop.Server.Tests.Unit --filter "FullyQualifiedName~ProductTests"
dotnet run --project aspire/Tnosc.EShop.AppHost        # Postgres + pgAdmin + Keycloak + host
dotnet run --project src/server/Tnosc.EShop.Server.Host  # API alone; needs ConnectionStrings__eshopdb

# EF — two contexts exist, so --context is always required (only the write context has migrations)
dotnet ef migrations add <Name> --context EShopWriteDbContext \
  --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
  --startup-project src/server/Tnosc.EShop.Server.Host
```

The Aspire Postgres resource uses `WithDataVolume()`, so a schema change in development may require
dropping the volume first. A feature is not done until the build is clean, the new unit and
integration tests are green, and the architecture tests still pass.
