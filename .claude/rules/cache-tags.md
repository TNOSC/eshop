# Rule — cache tags are constants, never literals

**Every `[CacheTag(...)]` argument is a `const` from `Server.Shared/<Context>/CacheTags.cs`.**
A string literal in a `[CacheTag(...)]` is a defect, not a style preference.

## Why

The two halves of the cache contract live in projects that cannot see each other:

| Half | Attribute | Project |
|---|---|---|
| Populates the cache | `[Cacheable(n)]` + `[CacheTag(...)]` on a query handler | `Server.Infrastructure.Persistence` |
| Invalidates it | `[CacheTag(...)]` on a command handler | `Server.Application` |

`CacheInvalidationDecorator` evicts by tag **string**. If the two sides spell it differently, nothing
fails: the build is clean, the tests that check each half in isolation pass, and the query handler
keeps serving a stale snapshot until its TTL expires. The failure is silent, delayed, and looks like
a caching bug rather than a typo.

`Server.Shared` is referenced by both projects, so a shared `const` makes the mismatch impossible —
a wrong member name is a compile error.

## How

```csharp
// src/server/Tnosc.EShop.Server.Shared/Catalog/CacheTags.cs
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

```csharp
using Tnosc.EShop.Server.Shared.Catalog;

[CacheTag(CacheTags.Catalog)]                     // command handler — invalidates
internal sealed class CreateProductCommandHandler(…)

[Cacheable(300)]
[CacheTag(CacheTags.Catalog)]                     // query handler — populates
internal sealed class GetCategoriesQueryHandler(…)
```

Attribute arguments must be compile-time constants, so `const string` is required — a `static
readonly` will not compile in this position.

## Scope and granularity

- **One class per bounded context**, at `Shared/<Context>/CacheTags.cs`. Contexts never share a tag
  class — that would couple them.
- Start with a **single context-wide tag** (`"catalog"`) and split only when eviction is measurably
  too broad. A tag per aggregate that every write handler carries anyway buys nothing.
- The tag's string value is internal to the process — it is not a wire contract, so it can be renamed
  freely, unlike `[DomainEventName]` (see `domain-events.md`).

## Checklist

- [ ] The tag is a `const string` in `Server.Shared/<Context>/CacheTags.cs`, with XML docs.
- [ ] Every `[Cacheable]` query handler has a `[CacheTag(...)]`.
- [ ] Every command handler mutating that data carries the **same** tag constant.
- [ ] No string literal appears in any `[CacheTag(...)]` anywhere.

Verify with `grep -rn 'CacheTag("' --include=*.cs src lib` — it must return nothing.

## Test coverage

`GetCategoriesCachingTests` covers both halves against a real database: a cached read served without
touching Postgres, then invalidation by a tagged write. A new context's first cached query deserves
the same pair — a unit test asserting the attribute is present proves only that someone typed it.
