---
name: add-context
description: Bootstrap a new bounded context in Tnosc.EShop — folder structure across all five projects, Postgres schema, first aggregate, first slice, migration, and test scaffolding. Use when the user asks to add a bounded context or module such as Basket, Ordering, Identity, or Payment.
argument-hint: <context name and purpose, e.g. "Basket — a customer's pending items">
---

# Add a Bounded Context

The planned contexts are **Catalog, Identity, Basket, Ordering, Payment** (`PLAN.md`, tasks T10–T14).
Catalog is built and is the reference. Check `PLAN.md` for the task file covering the context you are
adding — it records decisions already made, so do not relitigate them.

## Invariants

- **Contexts must not reference each other.** `LayerDependencyTests.Contexts_Should_Not_Depend_On_Each_Other`
  fails the build otherwise. Cross-context communication goes through domain events and the outbox,
  never a direct type reference. Duplicate the little you need rather than sharing an aggregate.
- **One Postgres schema per context** (`catalog`, `identity`, `basket`, `ordering`, `payment`, plus
  `outbox`), but **one database and one `DbContext` pair**. That is deliberate: the outbox insert must
  share the aggregate's transaction, and `TransactionDecorator` injects a single `IUnitOfWork`.
- A context is a **folder** in each existing project, not a new project.

## Folders to create

```
Server.Domain/<Context>/<Aggregate>s/{Aggregate,AggregateId,AggregateErrors,IAggregateRepository}.cs
Server.Domain/<Context>/<Aggregate>s/Events/
Server.Application/<Context>/Commands/<Feature>/
Server.Application/<Context>/Queries/<Feature>/
Server.Application/<Context>/EventHandlers/          # domain-event handlers, if any
Server.Infrastructure.Persistence/<Context>/{Context}Schema.cs
Server.Infrastructure.Persistence/<Context>/{Configurations,Queries,ReadModels,Repositories}/
Server.Api/<Context>/{Context}Routes.cs
Server.Api/<Context>/<Feature>/
tests/…Tests.Unit/<Context>/{Context}Faker.cs
tests/…Tests.Integration/<Context>/{Context}IntegrationTestBase.cs
```

## Workflow

1. **Schema constants** — `internal static class {Context}Schema` with `Name` and one `*Table`
   constant per table. Every `IEntityTypeConfiguration` in the context uses them; no inline strings.

   ```csharp
   internal static class BasketSchema
   {
       /// <summary>The Postgres schema every Basket table lives in.</summary>
       public const string Name = "basket";

       /// <summary>The name of the baskets table.</summary>
       public const string BasketsTable = "baskets";
   }
   ```

2. **Route constants** — `internal static class {Context}Routes` with `Tag` and one route template per
   endpoint, paths under `/api/<context>/…`. These stay in `Server.Api` — it does not reference
   `Server.Shared`.

3. **Cache tags** — `Server.Shared/<Context>/CacheTags.cs`, a `public static class CacheTags` with one
   `public const string` per tag. Required because the command handler that invalidates a tag lives in
   `Server.Application` and the query handler that populates it lives in
   `Server.Infrastructure.Persistence`; both reference `Server.Shared`, so the constant is the only
   thing keeping them in step. Never a string literal (`.claude/rules/cache-tags.md`).

   ```csharp
   namespace Tnosc.EShop.Server.Shared.Basket;

   /// <summary>Cache tags shared by the Basket bounded context's <c>[CacheTag]</c> handlers.</summary>
   public static class CacheTags
   {
       /// <summary>Tag covering every cached Basket query.</summary>
       public const string Basket = "basket";
   }
   ```

4. **First aggregate** — use the `add-entity` skill. Start with the root that gives the context its
   name; resist modelling the whole context up front.

5. **First slice** — use the `add-feature` skill. A create command plus a get-by-id query proves the
   rails end to end: DI scanning, EF configuration, the read model, the outbox, and the decorators.

6. **Migration** — one per context, named `{Context}_Initial`:

   ```bash
   dotnet ef migrations add Basket_Initial --context EShopWriteDbContext \
     --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
     --startup-project src/server/Tnosc.EShop.Server.Host
   ```

   Confirm the generated migration creates the schema (`basket`) before its tables, and touches no
   other context's objects.

7. **Test scaffolding** — a `{Context}Faker` (Bogus extensions for that context's data) and a
   `{Context}IntegrationTestBase : IntegrationTestBase` with the seed helpers the context's query
   tests need. Mirror `CatalogFaker` and `CatalogIntegrationTestBase`.

8. **Verify** — `dotnet build Tnosc.EShop.slnx`, then `dotnet test Tnosc.EShop.slnx`. Watch
   `Tests.Architecture` specifically: it will catch a cross-context reference or a misplaced handler
   immediately.

## No DI wiring needed

Handlers, validators, domain-event handlers, query handlers, repositories, EF configurations and
endpoints are all discovered by assembly scan. A new context adds **no** lines to
`ApplicationExtensions.cs`, `InfrastructurePersistenceExtensions.cs`, `ApiExtensions.cs` or
`Program.cs`. If you think you need to register something by hand, you have probably misnamed a type
or put it in the wrong project.

## Cross-context integration

When context B must react to something in context A: A's aggregate raises a domain event, the outbox
delivers it, and B handles it in its own `EventHandlers/` folder against **B's** types. B never
references A's aggregate, and A never calls into B.
