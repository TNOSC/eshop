# Rule — dependencies and packages

## Central Package Management

`Directory.Packages.props` sets `ManagePackageVersionsCentrally`. Therefore:

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Some.Package" Version="1.2.3" />

<!-- the consuming .csproj — no Version attribute -->
<PackageReference Include="Some.Package" />
```

**A `Version=` on a `PackageReference` is a build error.** Add the `PackageVersion` centrally, then
reference the package bare from each project that needs it.

Analyzer packages carry `<IncludeAssets>runtime; build; native; contentfiles; analyzers;
buildtransitive</IncludeAssets>`; test-only tooling (`coverlet.collector`,
`xunit.runner.visualstudio`) also carries `<PrivateAssets>all</PrivateAssets>`. Match the existing
entries rather than inventing a new shape.

## Adding a package needs a reason

Every dependency is permanent in practice. Before adding one:

1. **Is it in the BCL?** `System.Text.Json`, `TimeProvider`, `Guid.CreateVersion7()`,
   `System.Threading.Channels` cover a lot of what people reach for a package to do.
2. **Is it already referenced?** The solution already has Scrutor (DI scanning and decoration),
   HybridCache, Bogus, NSubstitute, Shouldly, Testcontainers, Respawn, NetArchTest.
3. **Does it fit the architecture?** A package that wants to own dispatching (MediatR), validation
   (FluentValidation) or mapping (AutoMapper) conflicts with deliberate decisions here — the custom
   `ICommandHandler` pipeline, `IValidator<T>` returning `Result`, and hand-written projections. Those
   choices are recorded in `PLAN.md`; do not reverse one by adding a package.

State the reason in the commit message. If the answer to "could we do this in 30 lines we control?"
is yes, prefer the 30 lines.

## Layer discipline applies to packages too

A package reference can violate the architecture as easily as a `using` can:

- **`Server.Domain`** takes no package that drags in EF Core, ASP.NET or Npgsql. It should stay
  referenceable from anywhere.
- **`Server.Application`** takes no EF Core or Infrastructure package.
- **`Server.Api`** takes no persistence package.

`LayerDependencyTests.Only_Persistence_Assemblies_Should_Depend_On_EfCore` catches the EF case;
the rest rely on this rule.

## The analyzers are not optional

Meziantou, SonarAnalyzer, Roslynator and xunit.analyzers are applied to every project from
`Directory.Build.props`. Do not remove one from a project to make it build, and do not add
`<NoWarn>` at the project level — see `analyzer-suppressions.md`.

## Upgrades

Bump the version in `Directory.Packages.props` only, then `dotnet build Tnosc.EShop.slnx` and
`dotnet test Tnosc.EShop.slnx`. An analyzer upgrade routinely surfaces new warnings — which are build
errors here, so treat the upgrade as its own change, not a drive-by inside a feature commit.

The solution targets `net10.0` on a preview SDK with no `global.json`; the SDK floats with what is
installed. Pin it deliberately if that becomes a problem, rather than as a side effect.
