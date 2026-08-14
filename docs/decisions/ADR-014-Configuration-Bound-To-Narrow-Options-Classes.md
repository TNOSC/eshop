# ADR-014: Configuration Bound To Narrow Options Classes, Read Once At Composition Root

## Status

Accepted

## Date

2026-08-14

## Context

Classes across Infrastructure, Api and Host need settings from `appsettings.json`. The common ways to
consume configuration in ASP.NET Core are constructor-injecting `IConfiguration` directly, constructor-
injecting `IOptions<TOptions>`, or constructor-injecting a plain POCO that was bound and validated once at
startup.

## Decision

No class constructor-injects `IConfiguration`, `IConfigurationSection`, or any `IOptions<TOptions>`/
`IOptionsSnapshot<TOptions>`/`IOptionsMonitor<TOptions>`. A class needing settings takes a plain `TOptions`
POCO, scoped to exactly the keys it needs, named `<Feature>Options` (never `*Settings`) and colocated with
the `AddXxx` extension method that registers it. `IConfiguration` and `IOptions<T>` are each touched in
exactly one place per settings class: inside that `AddXxx` method, via
`AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`, then unwrapped into a plain
singleton so every consumer only ever sees `TOptions`. This is mechanized:
`ConfigurationTests.No_Constructor_Should_Inject_IConfiguration`/`.No_Constructor_Should_Inject_IOptions`
scan every constructor across every project with Mono.Cecil.

## Rationale

- **`IConfiguration` in a constructor makes the class untestable without assembling the whole
  configuration surface** — a unit test must build or mock an `IConfiguration` instead of just
  `new SomeOptions { Timeout = ... }`, coupling every test of that class to how configuration is
  *assembled*, not just to the three values it actually reads.
- **No compile-time key safety.** A typoed key (`configuration["Catlog:PageSize"]`) does not fail to build
  and does not throw at startup — it silently returns `null`, parsing fails, and the class falls back to
  an unchosen default. The bug ships and looks like "the setting doesn't do anything" rather than "the key
  is misspelled."
- **`IConfiguration` in a constructor signature documents nothing** — it is the entire configuration tree,
  every section, every provider. A `TOptions` parameter documents exactly what the class depends on,
  because that's exactly what the class's own file declares.
- **`IOptions<T>` in a consumer constructor buys nothing this codebase uses.** `IOptionsSnapshot`/
  `IOptionsMonitor` reload-on-change is never used anywhere; the only thing `IOptions<T>` adds over a
  plain class is an `.Value` indirection every test has to unwrap. Unwrapping to a singleton at
  registration time gets the validation benefit of the Options pattern without paying that tax at every
  consumption site.
- **`.ValidateOnStart()` over "validate on first resolve" or "don't validate at all"** — a bad configured
  value crashes the app immediately at startup, before it accepts traffic, rather than surfacing as a
  mysteriously wrong default discovered days later or a failure on the first request that happens to hit
  that code path.
- Alternative rejected: `IOptions<TOptions>` injected directly into consumers (the ASP.NET Core default
  pattern) — rejected because this codebase never needs the reload-on-change capability `IOptions<T>`
  exists for, and the `.Value` indirection it adds is pure test friction with no offsetting benefit here.

## Consequences

**Easier:**
- Every settings-consuming class is unit-testable with a directly constructed POCO — no `IConfiguration`
  mock, no `Options.Create(...)` wrapper, no DI container involved in the test.
- A misconfigured or missing required value fails the host at startup with the offending property name in
  the message, not as a silent wrong default discovered later.

**Harder:**
- Every settings class needs its own `AddOptions<T>().Bind(...).ValidateOnStart()` registration boilerplate
  in its owning `AddXxx` method, rather than a single line resolving `IOptions<T>` wherever needed.
- `lib/` framework projects intentionally use a different pattern (delegate-populated Options, e.g.
  `OutboxOptions`) since they have no JSON source of truth of their own — contributors must know which
  pattern applies where rather than one rule applying uniformly everywhere.
