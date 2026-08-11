# Shared

Constants and primitives that more than one server project must agree on, organised **per bounded
context** (`Shared/<Context>/…`), mirroring the folder layout everywhere else.

Referenced by `Server.Domain`, `Server.Application`, `Server.Infrastructure.Persistence` and
`Tests.Architecture`. **`Server.Api` does not reference it** — route templates and the OpenAPI tag
therefore stay in `Server.Api/<Context>/{Context}Routes.cs`, and schema/table names stay in
`…Persistence/<Context>/{Context}Schema.cs`.

## What belongs here

A value only earns a place in `Shared` when **two projects that cannot see each other** must resolve
the identical literal. Cache tags are the motivating case:

- `[CacheTag(...)]` on a command handler in `Server.Application` **invalidates** the tag.
- `[CacheTag(...)]` on a query handler in `Server.Infrastructure.Persistence` **populates** it.

Neither project references the other, so a string literal in each is two independent literals. A typo
does not fail the build — the cache simply stops being invalidated, and the bug surfaces later as
stale reads. The constant is the only thing keeping the two sides in step.

```csharp
namespace Tnosc.EShop.Server.Shared.Catalog;

/// <summary>
/// Cache tags shared by the Catalog bounded context's <c>[CacheTag]</c> handlers, so the write
/// handlers that invalidate and the query handlers that populate the cache cannot drift apart.
/// </summary>
public static class CacheTags
{
    /// <summary>
    /// Tag covering every cached Catalog query — invalidated by every Catalog write handler.
    /// </summary>
    public const string Catalog = "catalog";
}
```

Full policy: `.claude/rules/cache-tags.md`.

## What does not belong here

- **Anything with behaviour.** `Shared` is constants and inert primitives. Business rules belong to
  the domain; orchestration to the application.
- **Types one project alone uses** — keep them next to their use.
- **Anything that would let two bounded contexts couple.** `Shared` is organised per context
  precisely so `Basket` cannot reach for a `Catalog` constant; contexts still communicate only
  through domain events and the outbox.

## Conventions

- `public static class`, `public const string` members, XML docs on every member.
- One file per concern, named for it (`CacheTags.cs`), under `Shared/<Context>/`.
- Same file header, explicit `using`s and file-scoped namespace as everywhere else.
