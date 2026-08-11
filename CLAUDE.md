# CLAUDE.md

Clean Architecture / DDD / CQRS eShop built on the in-repo `lib/` framework. Authoritative design docs:
[`# Clean Architecture Rules & Design.md`](./%23%20Clean%20Architecture%20Rules%20%26%20Design.md) and
[`plan/00-conventions.md`](./plan/00-conventions.md); [`PLAN.md`](./PLAN.md) tracks delivery order.

**Scoped rules live in nested `CLAUDE.md` files — read the one for the tree you are editing:**
`lib/` · `src/server/Tnosc.EShop.Server.Domain/` · `…Server.Application/` ·
`…Server.Infrastructure.Persistence/` · `…Server.Api/` · `…Server.Shared/` · `tests/`

## Tech stack

| | Version | | Version |
|---|---|---|---|
| TFM / SDK | `net10.0` / 10.0.400-preview (no `global.json`) | Aspire | 13.4.6 (+ Hosting.PostgreSQL, Npgsql.EFCore) |
| ASP.NET Core | 10.0.10 — Minimal APIs only, no MVC | Scrutor | 7.0.0 (`Scan` + `TryDecorate`) |
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
aspire/                  AppHost (Postgres + pgAdmin resources) · ServiceDefaults (OTel, health, resilience)
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

**Configuration and options:** See [`.claude/rules/configuration-options.md`](./.claude/rules/configuration-options.md) — any
settings read from `appsettings.json` go through a narrow, type-safe `<Feature>Options` class, never
`IConfiguration` in a consumer's constructor. `IConfiguration` is touched exactly once, in the owning
`AddXxx` extension method, to bind and validate.

Every `.cs` file opens with this header, then explicit `using`s (System first), then a **file-scoped
namespace**. One public type per file, named after the file.

```csharp
// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------
```

- **Explicit types, not `var`** — except where the type is apparent (`new Product { … }`, `new()`).
- **Name every argument at every call site** — each parameter's name is written out on every method,
  constructor and factory call. No positional arguments, tests included; only `params` arrays are
  exempt: `Money.Create(amount: x, currency: y)`, `ShouldBe(expected: "Product.NotFound")`.
- Braces always, even one-line `if`. `static` lambdas where nothing is captured. CRLF, 4 spaces.
- Expression-bodied properties/accessors/operators/lambdas yes; multi-statement methods use blocks.
- Error codes are `Aggregate.Reason`: `Product.NotFound`, `Sku.InvalidFormat`.
- `ErrorType` → HTTP: `Validation` 400 · `Unauthorized` 401 · `Forbidden` 403 · `NotFound` 404 ·
  `Conflict` 409 · `Failure`/`Unexpected` 500 · `Custom` → its `NumericType`.

## Commands

```bash
dotnet build Tnosc.EShop.slnx                          # warnings are errors; clean build = done
dotnet test  Tnosc.EShop.slnx                          # integration suite needs Docker running
dotnet test  tests/server/Tnosc.EShop.Server.Tests.Unit --filter "FullyQualifiedName~ProductTests"
dotnet run --project aspire/Tnosc.EShop.AppHost        # Postgres + pgAdmin + host
dotnet run --project src/server/Tnosc.EShop.Server.Host  # API alone; needs ConnectionStrings__eshopdb

# EF — two contexts exist, so --context is always required (only the write context has migrations)
dotnet ef migrations add <Name> --context EShopWriteDbContext \
  --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
  --startup-project src/server/Tnosc.EShop.Server.Host
```

The Aspire Postgres resource uses `WithDataVolume()`, so a schema change in development may require
dropping the volume first. A feature is not done until the build is clean, the new unit and
integration tests are green, and the architecture tests still pass.
