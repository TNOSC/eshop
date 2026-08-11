---
name: add-entity
description: Add a domain aggregate to Tnosc.EShop end to end — aggregate, strongly-typed id, value objects, error catalog, domain events, repository contract, EF configuration, read model, and migration. Use when the user asks to add an entity, aggregate, domain model, or table.
argument-hint: <aggregate description, e.g. "Review with a rating, author and product">
---

# Add a Domain Aggregate

Create an aggregate and wire it through every layer, following the `Product` aggregate in `Catalog`.
Read `src/server/Tnosc.EShop.Server.Domain/CLAUDE.md` first — this is a **rich** domain, not a bag of
properties.

## Decide first

- **Aggregate root or child entity?** If it has its own lifecycle and is loaded on its own, it is a
  root: `AggregateRoot<TId>` + its own repository contract. Otherwise it belongs inside an existing
  root and has no repository.
- **What is a value object?** Anything with rules but no identity — money, a SKU, a quantity, an
  email. Give it a `Result<T> Create(...)` factory; do not scatter its validation across handlers.
- **Which invariants span the aggregate?** Uniqueness across a table cannot be decided by one
  instance — that rule goes in a static `{Aggregate}Factory` that takes the repository contract.

## Files to create

All under `src/server/Tnosc.EShop.Server.Domain/<Context>/<Aggregate>s/` unless noted. Every file
opens with the TNOSC copyright header, then explicit `using`s, then a file-scoped namespace.
**XML docs on every public member** — analyzers are warnings-as-errors.

### 1. Strongly-typed id

```csharp
public sealed record ReviewId : GuidEntityId, IEntityId<ReviewId, Guid>
{
    private ReviewId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier backed by a time-ordered (version 7) GUID.</summary>
    public static ReviewId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static ReviewId From(Guid value) => new(value);
}
```

### 2. Value objects (as needed)

`public sealed record X : ValueObject` with a **private constructor** and a public
`static Result<X> Create(...)` factory — an architecture test enforces exactly that pairing.

### 3. Error catalogue — `{Aggregate}Errors.cs`

`public static class`, one member per failure, codes are `{Aggregate}.{Reason}`. The `ErrorType`
chosen here decides the HTTP status, so pick it by semantics:
`Error.Validation` 400 · `Error.Unauthorized` 401 · `Error.Forbidden` 403 · `Error.NotFound` 404 ·
`Error.Conflict` 409 · `Error.Failure`/`Error.Unexpected` 500.

```csharp
public static class ReviewErrors
{
    /// <summary>No review carries the requested identifier.</summary>
    /// <param name="reviewId">The identifier that was looked up.</param>
    public static Error NotFound(Guid reviewId) => Error.NotFound(
        code: "Review.NotFound",
        description: $"Review {reviewId} was not found.");

    /// <summary>Gets the error returned when a rating is out of range.</summary>
    public static Error RatingOutOfRange => Error.Validation(
        code: "Review.RatingOutOfRange",
        description: "A rating must be between 1 and 5.");
}
```

### 4. Domain events — `Events/{Aggregate}{PastTenseVerb}DomainEvent.cs`

One record per file, carrying **flat primitives and ids**, never entities. The
`[DomainEventName]` value must be unique solution-wide (architecture test) — version the suffix
rather than renaming an existing one.

```csharp
[DomainEventName("catalog.review-posted.v1")]
public sealed record ReviewPostedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ReviewId,
    Guid ProductId,
    int Rating) : IDomainEvent;
```

### 5. Aggregate

`public sealed class Review : AggregateRoot<ReviewId>` — private EF constructor, `private set`
properties, static `Result<T>` factory, behaviour methods that own their transitions. Every method
that mutates state calls `IncrementVersion()` — **including `Create`** — and raises its domain event.
See the canonical example in `Server.Domain/CLAUDE.md`.

Make `Create` `internal` if a `{Aggregate}Factory` must guard a rule that spans the aggregate.

### 6. Repository contract (aggregate roots only)

In the **Domain** project, next to the aggregate — that placement is deliberate, so a domain factory
can enforce uniqueness without pushing a business `if` into a handler.

```csharp
public interface IReviewRepository : IRepository<Review, ReviewId>
{
    ValueTask<Review?> GetByProductAndAuthorAsync(
        ProductId productId, CustomerId authorId, CancellationToken cancellationToken = default);
}
```

`IRepository<,>` already gives `GetByIdAsync`, `AddAsync`, `Update`, `Remove`. Add only methods that
express business intent.

## Wire it into Infrastructure

### 7. EF configuration — `…Persistence/<Context>/Configurations/{Aggregate}Configuration.cs`

`internal sealed : IEntityTypeConfiguration<T>`. Table and columns are `snake_case`, named
explicitly. Value objects map with `OwnsOne`. **No `HasConversion` for typed ids** —
`EntityIdConventions` registers those pre-convention, foreign keys included. Finish with
`builder.ConfigureAggregateRootColumns()`.

Aggregates hold no navigation properties, but the FK constraint still belongs in the database:
`builder.HasOne<Product>().WithMany().HasForeignKey(…).OnDelete(DeleteBehavior.Restrict)`.

Add the table name to the context's `{Context}Schema` constants class.

### 8. Repository implementation — `…Persistence/<Context>/Repositories/{Aggregate}Repository.cs`

```csharp
internal sealed class ReviewRepository(EShopWriteDbContext context)
    : RepositoryBase<Review, ReviewId>(context), IReviewRepository
```

Registered automatically by the Scrutor scan — **do not** edit `InfrastructurePersistenceExtensions`.

### 9. Read model + configuration (if the aggregate is queried)

`internal sealed {Aggregate}ReadModel : IReadModel` in `<Context>/ReadModels/` — flat primitives, no
typed ids, no value objects — plus its own `IEntityTypeConfiguration<>` mapping it to the same table.

### 10. Migration

```bash
dotnet ef migrations add Add_Reviews --context EShopWriteDbContext \
  --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
  --startup-project src/server/Tnosc.EShop.Server.Host
```

Names are `PascalCase_With_Underscores` (`Catalog_Initial`, `Add_Reviews`). Only the write context
has migrations. `dotnet ef` runs outside Aspire and picks up its connection string from
`EShopWriteDbContextFactory`. **Read the generated migration** before committing — a surprise drop or
rename means the configuration is wrong.

## Tests

- Unit-test the aggregate's factory, every transition, and every value object — one test per
  invariant, asserting the error code (`tests/CLAUDE.md`).
- If you added a read model, integration-test the projection.
- Run `dotnet build Tnosc.EShop.slnx` and `dotnet test Tnosc.EShop.slnx`; `Tests.Architecture`
  checks typed-id shape, value-object shape, no public setters, and unique domain-event names.

If the user also wants use cases for the aggregate, continue with the `add-feature` skill.
