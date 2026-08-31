---
description: "Configuration is read once, into a narrow Options class; every consumer takes the plain TOptions, never IConfiguration or IOptions<T>"
applyTo: "src/**/*Options.cs,src/**/*Extensions.cs,lib/**/*Options.cs,lib/**/*Extensions.cs"
---

# Rule — configuration is read once, into a narrow `Options` class

**No class constructor-injects `IConfiguration`, `IConfigurationSection`, or `IOptions<TOptions>`.**
A class that needs settings from `appsettings.json` takes the plain `TOptions` class itself as a
constructor parameter, where `TOptions` is a POCO scoped to exactly the keys that class needs.
`IConfiguration` is read in exactly one place per settings class — the project's own `AddXxx`
extension method, at composition-root binding time — and `IOptions<TOptions>` is touched only
inside that same method, to validate the bound value before the app finishes starting.

**Mechanised, not just documented.** `ConfigurationTests.No_Constructor_Should_Inject_IConfiguration`
and `.No_Constructor_Should_Inject_IOptions` in `Tests.Architecture` scan every constructor across
Domain, Application, Api, Infrastructure.*, Shared and Host with Mono.Cecil — a violation fails the
build. `AddXxx` extension methods are static methods on static classes, not constructors, so the one
sanctioned binding site is untouched by the scan; anything else that takes `IConfiguration` or an
`IOptions<T>`/`IOptionsSnapshot<T>`/`IOptionsMonitor<T>` as a constructor parameter fails immediately.

## Why

A class holding `IConfiguration` has three problems that only surface later, never at the call site:

- **Untestable without the whole configuration surface.** A unit test for the class must build (or
  mock) an `IConfiguration` — usually via `ConfigurationBuilder().AddInMemoryCollection(...)` — instead
  of just `new SomeOptions { Timeout = ... }`. Every test of that class now carries a dependency on how
  configuration is *assembled*, not just on the three values it actually reads.
- **No compile-time key safety.** `configuration["Catlog:PageSize"]` (typoed) does not fail to build
  and does not throw at startup — it silently returns `null`, `int.TryParse` fails, and the class falls
  back to a default no one chose. The bug ships, and it looks like "the setting doesn't do anything"
  rather than "the key is misspelled".
- **An undocumented, unbounded dependency.** `IConfiguration` is the entire configuration tree — every
  section, every provider, environment variables, command-line args, user secrets. A constructor
  parameter of that type tells a reader nothing about what the class actually needs; a constructor
  parameter of `TOptions` tells a reader exactly two lines of settings exist, because that is exactly
  what the class's own file declares.

`IOptions<TOptions>` in a consumer's constructor is a smaller version of the same problem, and this
repo has no use for what it buys: `IOptionsSnapshot`/`IOptionsMonitor` reload-on-change is never used
anywhere today, so the only thing `IOptions<T>` adds over the plain class is an `.Value` indirection
that every unit test has to unwrap — `new SomeHandler(Options.Create(new SomeOptions{...}))` instead
of `new SomeHandler(new SomeOptions{...})`. A plain `TOptions` constructor parameter is a POCO a test
constructs directly, a typoed JSON key is caught by a startup validation failure (see **Validation**
below) instead of a silent default, and the constructor signature *is* the documentation of what the
class depends on from config.

## How

**Naming and location.** `<Feature>Options.cs`, suffixed `Options` — never `Settings` (no
`*Settings.cs` exists in this repo; do not introduce the alternate suffix). It is colocated with the
feature that owns it, next to the `AddXxx` extension method that registers it — mirroring `lib/`'s
existing `OutboxOptions`/`IdempotencyOptions` colocation, not centralized in `Server.Shared`.

**Section-name convention.** The class name minus the `Options` suffix is the JSON section name,
made explicit with a `SectionName` const so the binding call and the class agree by construction
rather than by two people typing the same string in two files:

```csharp
// src/server/Tnosc.EShop.Server.Api/Catalog/SearchProducts/CatalogSearchOptions.cs
namespace Tnosc.EShop.Server.Api.Catalog.SearchProducts;

/// <summary>
/// Bounds how <see cref="SearchProductsEndpoint"/> paginates a catalog search, bound from the
/// <c>"CatalogSearch"</c> configuration section.
/// </summary>
public sealed class CatalogSearchOptions
{
    /// <summary>The configuration section this class binds to.</summary>
    public const string SectionName = "CatalogSearch";

    /// <summary>Gets or sets the largest page size a caller may request. Defaults to 100.</summary>
    [Range(1, 500)]
    public int MaxPageSize { get; set; } = 100;
}
```

```json
// appsettings.json
{
  "CatalogSearch": { "MaxPageSize": 100 }
}
```

**Registration — the one place `IConfiguration` and `IOptions<T>` are touched.** Inside the
project's own `AddXxx` extension method, at the composition-root boundary. Bind through
`AddOptions<T>()` so validation runs, force that validation to happen at app startup rather than on
first resolve, then unwrap `IOptions<T>` into the plain class so every other registration and every
consumer only ever sees `CatalogSearchOptions`:

```csharp
// src/server/Tnosc.EShop.Server.Api/Extensions/ApiExtensions.cs
public static IServiceCollection AddApiEndpoints(this IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<CatalogSearchOptions>()
        .Bind(configuration.GetSection(CatalogSearchOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddSingleton(resolve => resolve.GetRequiredService<IOptions<CatalogSearchOptions>>().Value);
    // ... existing registrations
}
```

`.ValidateOnStart()` itself is what makes validation eager: it registers the options type with a
hosted service (`IStartupValidator`) that runs during host startup and validates every
`ValidateOnStart()`-registered options type up front — a bad value crashes the app immediately,
before it accepts any traffic, with no need for anything to resolve the option first. The unwrapped
singleton line above exists for a different reason — so `IOptions<T>` never leaks into a consumer's
constructor — not to trigger validation, which `.ValidateOnStart()` already guarantees on its own.

`configuration.GetSection(...)` narrows the tree before binding, and this block is the only place in
the codebase that mentions `CatalogSearchOptions` together with `IConfiguration` or `IOptions<T>`.
Everywhere else — every consumer, every other registration — only the plain class name appears.

**Consumption — never `IConfiguration`, never `IOptions<T>`.** The consumer's constructor takes the
plain class directly:

```csharp
internal sealed class SearchProductsEndpoint(CatalogSearchOptions options) : IApiEndpoint
{
    // options.MaxPageSize, etc.
}
```

A unit test for `SearchProductsEndpoint` constructs `new CatalogSearchOptions { MaxPageSize = 10 }`
directly — no `Options.Create(...)` wrapper, no `IConfiguration` mock, no DI container involved.

**Validation.** Add `[Range]`/`[Required]`/etc. from `System.ComponentModel.DataAnnotations` and chain
`.ValidateDataAnnotations().ValidateOnStart()` on every new `Options` class bound from JSON, so a bad
or missing value fails the host at startup with the offending property name in the message, instead
of surfacing as a mysteriously-wrong default discovered days later. This repo already runs with
`Nullable=enable` discipline and DataAnnotations attributes are not touched by any `.editorconfig`
suppression, so there is no analyzer friction. This needs one new package reference —
`Microsoft.Extensions.Options.DataAnnotations` — added to `Directory.Packages.props` the first time
this rule's validation clause is used; `Microsoft.Extensions.Options.ConfigurationExtensions` (for
`.Bind()`/`GetSection()`) is already present.

**Layer placement.** Settings classes bound from `appsettings.json` belong in **Infrastructure, Api,
or Host** — the layers that already own an `AddXxx` composition-root extension method and already may
depend on `Microsoft.Extensions.Options`/`Configuration`. They do not belong in Domain or Application:
Domain has no framework dependency at all, and Application's `AddApplication()` has never needed
config (see `LayerDependencyTests`). `ConfigurationTests` (above) already stops the most likely form
of a violation — Domain or Application code cannot constructor-inject `IConfiguration`/`IOptions<T>`
anywhere, because that scan covers every assembly including those two. What is **not** mechanised is
a settings class merely *living* in Domain or Application without being injected anywhere yet —
`LayerDependencyTests.Domain_Should_Not_Depend_On_OuterLayers` and
`Application_Should_Not_Depend_On_Infrastructure_Api_Host_Or_Web_Frameworks` forbid EF Core, ASP.NET
Core, Npgsql and the outer-layer assemblies, but neither list mentions
`Microsoft.Extensions.Configuration` or `Microsoft.Extensions.Options`. If that gap is ever hit in
review, add `Microsoft.Extensions.Configuration` to both forbidden-dependency lists in
`LayerDependencyTests.cs` rather than special-casing the one offending class.

## Relationship to lib/'s delegate-populated Options (OutboxOptions, IdempotencyOptions, PersistenceOptions)

Two legitimate patterns for two different situations — this rule does not contradict or deprecate the
other:

- **`lib/` framework code has no JSON source of truth of its own.** `Tnosc.Lib.Infrastructure.Persistence`
  is consumed by any host, with or without an `appsettings.json` in the shape it expects. `PersistenceOptions`
  (and the `OutboxOptions`/`IdempotencyOptions` it aggregates) are populated by a caller-supplied
  `Action<PersistenceOptions> configure` delegate precisely so `lib/` never touches `IConfiguration` at
  all — keeping it config-source-agnostic is the whole point of a reusable framework project.
- **`src/server/` application code has exactly one `appsettings.json`, in this repo, with a known
  shape.** There is no reason to route a `CatalogSearchOptions` through a hand-written delegate when
  `services.AddOptions<T>().Bind(configuration.GetSection(...))` says the same thing with less code and
  gives operators the usual "change the JSON, no redeploy of a delegate" experience.

If a `src/server/` class needs to feed a `lib/` delegate with a value that itself came from JSON —
exactly what `InfrastructurePersistenceExtensions.AddInfrastructurePersistence` does today for
`options.ApplyMigrationsOnStartup` — that is composition-root code binding config into a plain value
and handing it to a delegate; it is not a violation of this rule, because the composition root is
exactly where `IConfiguration` is allowed to be touched. This rule does not require rewriting that call
site to introduce a one-property `Options` class for a single boolean — a `TOptions` class earns its
place once a consumer other than the composition root itself needs the value, or once there is more
than one related key to keep together.

## Checklist

- [ ] The settings class is named `<Feature>Options` (never `*Settings`), colocated with the `AddXxx`
      extension method that registers it — not centralized in `Server.Shared`.
- [ ] The class exposes a `SectionName` const, and the JSON section uses that name.
- [ ] `IConfiguration`/`GetSection(...)` and `IOptions<T>` both appear in exactly one place: the
      `AddXxx` extension method's `AddOptions<T>().Bind(...).ValidateOnStart()` +
      unwrap-to-singleton block.
- [ ] Every consumer's constructor takes the plain `TOptions` class directly — never
      `IConfiguration`, `IConfigurationSection`, or `IOptions<TOptions>`.
- [ ] Bound properties that must hold a value carry a DataAnnotation, and registration chains
      `.ValidateDataAnnotations().ValidateOnStart()`, so a bad value fails the host at startup.
- [ ] The class lives in Infrastructure, Api, or Host — never Domain or Application.

The second-to-last item is enforced by `ConfigurationTests` in `Tests.Architecture` — a violation
fails `dotnet test tests/server/Tnosc.EShop.Server.Tests.Architecture`, no manual grep needed.
