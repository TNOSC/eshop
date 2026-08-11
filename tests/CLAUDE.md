# Tests

| Suite | Covers | Tools |
|---|---|---|
| `Tests.Unit` | Domain factories, entities, value objects, strategies, invariants; **command handlers**; `lib/` units | xUnit, NSubstitute, Shouldly, Bogus |
| `Tests.Integration` | **Query handlers**, EF projections, `UnitOfWork`, outbox processor, auditing | Testcontainers Postgres + Respawn |
| `Tests.Architecture` | The layering, naming and no-branching rules, mechanically | NetArchTest + Roslyn + Cecil |
| `Tests.Acceptance` | End-to-end over HTTP | Aspire.Hosting.Testing (not yet populated) |

That split is the rule, not a habit: domain and command logic are isolated and fast; queries are
validated against a real database. Never unit-test a query handler with a fake context, and never
reach for the EF in-memory provider.

## Conventions

- Naming: `MethodOrScenario_Should_ExpectedOutcome_When_Condition` — e.g.
  `HandleAsync_Should_PropagateTheConflict_Unchanged_When_TheSkuIsAlreadyTaken`. `CA1707` is
  suppressed, so underscores are fine.
- Test classes are `public sealed`. `Xunit` is a global `<Using>` in each test `.csproj`.
- **Shouldly, not `Assert`**: `result.IsSuccess.ShouldBeTrue()`,
  `result.FirstError.Code.ShouldBe(expected: "Product.SkuAlreadyExists")`. Pass
  `customMessage:` on collection assertions that would otherwise fail opaquely.
- **NSubstitute** over the domain-owned contracts (`Substitute.For<IProductRepository>()`,
  `Substitute.For<IUnitOfWork>()`). Don't mock EF or the framework.
- **Bogus** for data, via the per-context faker extensions (`_faker.Sku()`, `_faker.PriceAmount()`);
  build aggregates through the local `*TestFactory`, never by reaching into private state.
- `// Arrange` / `// Act` / `// Assert` comment blocks are the house style.
- XML docs are off here (`tests/Directory.Build.props`), but analyzers and warnings-as-errors still apply.

## Unit tests

Assert that a handler **propagates** the domain's verdict rather than restating the rule: same
`ErrorType`, same error code, and no commit on failure.

```csharp
result.IsError.ShouldBeTrue();
result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
result.FirstError.Code.ShouldBe(expected: "Product.SkuAlreadyExists");
await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
```

## Integration tests

**Docker must be running.** Derive from `IntegrationTestBase`
(`[Collection(nameof(PostgresCollection))]`): it resets every table with Respawn and opens a fresh
`AsyncServiceScope` per test, exposing `WriteContext`, `ReadContext`, `UnitOfWork`,
`OutboxProcessor`, `TimeProvider` (manually advanced) and `Spy`.

Save through **`UnitOfWork`**, not `WriteContext` directly — audit stamping and the
domain-event-to-outbox conversion only happen on that path, so writing through the context bypasses
exactly what the test is usually there to prove.

## Architecture tests

Treat a failure here as a design error, not a test to relax. They enforce layer dependencies,
context isolation, EF Core staying behind repositories, handler naming/sealing/placement, no
`DbContext` in command handlers, no `I*Repository` in query handlers, endpoint shape, aggregate
setters, typed-id and value-object shape, unique domain-event names, and the Roslyn no-business-
branching scan. Add a test here whenever you add a rule.

```bash
dotnet test tests/server/Tnosc.EShop.Server.Tests.Unit
dotnet test tests/server/Tnosc.EShop.Server.Tests.Integration   # needs Docker
dotnet test tests/server/Tnosc.EShop.Server.Tests.Architecture
```
