# Test Templates

The split is a rule, not a habit: **command handlers and the domain are unit-tested; query handlers
are integration-tested against real Postgres.** Never unit-test a query handler with a fake context,
and never reach for the EF in-memory provider.

| Suite | Covers | Tools |
|---|---|---|
| `Tests.Unit` | Domain factories, entities, value objects, strategies; command handlers | xUnit, NSubstitute, Shouldly, Bogus |
| `Tests.Integration` | Query handlers, EF projections, `UnitOfWork`, outbox | Testcontainers Postgres + Respawn |

Naming: `MethodOrScenario_Should_ExpectedOutcome_When_Condition`. Classes are `public sealed`.
`Xunit` is a global `<Using>`. `// Arrange` / `// Act` / `// Assert` blocks in every test.
Shouldly, never `Assert`. Name every argument, tests included.

## Command handler unit test

Substitute the **domain-owned** contracts (`IProductRepository`, `IUnitOfWork`) — never mock EF.
Build data with the context's Bogus faker; build aggregates through the local `*TestFactory`, never
by reaching into private state.

The point of these tests is that the handler **orchestrates and propagates** — assert the same
`ErrorType` and error code the domain chose, and that nothing was committed on failure.

```csharp
public sealed class UpdateProductPriceCommandHandlerTests
{
    private readonly Faker _faker = CatalogFaker.New();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task HandleAsync_Should_RepriceTheProductAndCommit_When_TheCommandIsValid()
    {
        // Arrange
        Product product = await ProductTestFactory.CreateAsync(sku: _faker.Sku());
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: product));

        // Act
        Result result = await HandleAsync(command: ValidCommand());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _repository.Received(requiredNumberOfCalls: 1).Update(aggregate: product);
        await _unitOfWork.Received(requiredNumberOfCalls: 1)
            .SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_TheProductDoesNotExist()
    {
        // Arrange
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result result = await HandleAsync(command: ValidCommand());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Product.NotFound");
        await _unitOfWork.DidNotReceive()
            .SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheDomainVerdict_Unchanged_When_TheProductIsDiscontinued()
    {
        // Arrange — the rule lives in Product.ChangePrice; the handler must not restate it
        Product product = await ProductTestFactory.DiscontinuedAsync();
        _repository
            .GetByIdAsync(id: Arg.Any<ProductId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: product));

        // Act
        Result result = await HandleAsync(command: ValidCommand());

        // Assert
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Product.Discontinued");
    }

    private UpdateProductPriceCommand ValidCommand() =>
        new(ProductId: Guid.CreateVersion7(),
            Amount: _faker.PriceAmount(),
            Currency: _faker.Currency());

    private ValueTask<Result> HandleAsync(UpdateProductPriceCommand command) =>
        new UpdateProductPriceCommandHandler(repository: _repository, unitOfWork: _unitOfWork)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
```

**Cover:** the happy path (state changed + committed), every guard the handler propagates (one per
`Errors.ToArray()` return), and "does not commit on failure".

## Domain unit test

One class per aggregate/value object. Assert the invariant, not the implementation.

```csharp
[Fact]
public void Create_Should_ReturnInvalidCurrency_When_TheCodeIsNotThreeUppercaseLetters()
{
    // Act
    Result<Money> result = Money.Create(amount: 10m, currency: "usd");

    // Assert
    result.IsError.ShouldBeTrue();
    result.FirstError.Code.ShouldBe(expected: "Money.InvalidCurrency");
}

[Fact]
public void ChangePrice_Should_RaiseTheDomainEvent_And_IncrementTheVersion()
{
    // Arrange
    Product product = ProductTestFactory.Create();
    int versionBefore = product.Version;

    // Act
    product.ChangePrice(newPrice: Money.Create(amount: 25m, currency: "EUR").Value);

    // Assert
    product.Version.ShouldBeGreaterThan(expected: versionBefore);
    product.DomainEvents.ShouldContain(elementPredicate: e => e is ProductPriceChangedDomainEvent);
}
```

## Query handler integration test

**Docker must be running.** Derive from the context's integration base (which derives from
`IntegrationTestBase`, `[Collection(nameof(PostgresCollection))]`): it resets every table with Respawn
and opens a fresh `AsyncServiceScope` per test, exposing `WriteContext`, `ReadContext`, `UnitOfWork`,
`OutboxProcessor`, `TimeProvider` and `Spy`.

**Seed through `UnitOfWork`, not `WriteContext` directly** — audit stamping and the
domain-event-to-outbox conversion only happen on that path, so writing through the context bypasses
exactly what the test usually exists to prove.

```csharp
public sealed class GetProductByIdQueryHandlerTests(PostgresFixture fixture)
    : CatalogIntegrationTestBase(fixture)
{
    [Fact]
    public async Task HandleAsync_Should_ReturnTheProjectedProduct_When_ItExists()
    {
        // Arrange
        Product product = await SeedProductAsync();

        // Act
        Result<ProductDto> result = await HandleAsync(
            query: new GetProductByIdQuery(ProductId: product.Id.Value));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Sku.ShouldBe(expected: product.Sku.Value);
        result.Value.PriceAmount.ShouldBe(expected: product.Price.Amount);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_NoProductCarriesTheId()
    {
        // Act
        Result<ProductDto> result = await HandleAsync(
            query: new GetProductByIdQuery(ProductId: Guid.CreateVersion7()));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
    }
}
```

**Cover:** the projection maps every column correctly, the not-found path, filtering/paging/ordering
for a search query, and — for raw SQL — that the join returns the joined columns rather than nulls.

## Checklist before finishing

- `dotnet build Tnosc.EShop.slnx` — warnings are errors.
- `dotnet test Tnosc.EShop.slnx` — including `Tests.Architecture`, which enforces handler placement,
  sealing, naming, and the no-business-branching scan.
- A new bounded context, aggregate, or handler shape usually needs a matching architecture-test
  update rather than a suppression.
