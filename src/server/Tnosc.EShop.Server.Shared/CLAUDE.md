# Shared

Constants and primitives that more than one server project must agree on, organised **per bounded
context** (`Shared/<Context>/…`), mirroring the folder layout everywhere else — plus the one
cross-cutting folder described below.

Referenced by `Server.Domain`, `Server.Application`, `Server.Infrastructure.Persistence`,
`Server.Api`, `Server.Host`, `Tests.Architecture` — and, outside `src/server/`, by `Mcp.Tool`
(`Authorization/Permissions.cs`, `Catalog/McpToolNames.cs`) and `Agent.Domain`
(`Catalog/McpToolNames.cs`). `Server.Shared` is a leaf project with no package reference of its own,
so none of those outer consumers pick up anything beyond the constants themselves.

`Server.Api` gained its reference in T11, for `Authorization/Permissions.cs`. That does **not** make
Shared a general dumping ground for Api constants: route templates and the OpenAPI tag still stay in
`Server.Api/<Context>/{Context}Routes.cs`, and schema/table names still stay in
`…Persistence/<Context>/{Context}Schema.cs`. Both are used by exactly one project, so neither
qualifies.

## The one non-context folder: `Authorization/`

`Authorization/{Permissions,Roles,RolePermissions}.cs` sits outside the per-context layout because
authorization genuinely is cross-cutting — one vocabulary spans every context, and a
`Shared/Catalog/Permissions.cs` plus a `Shared/Identity/Permissions.cs` would have to be kept in step
by hand.

It earns its place for exactly the `CacheTags` reason. `Server.Api` **names** a permission on an
endpoint via `HasPermission(...)`; `Server.Host`'s `KeycloakClaimsTransformation` **grants** it by
expanding a realm role through `RolePermissions`. Those two projects cannot see each other, so a
literal in each is two independent literals — and a typo does not fail the build, it fails at runtime
as a 403 that reads like a permissions bug rather than a spelling mistake.

One caveat that does not apply to cache tags: `Roles.Admin` / `Roles.Customer` must equal the realm
role names in `aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json`. A cache tag's value is internal to
the process and can be renamed freely; a role name is a contract with Keycloak and cannot.

Full policy: `.claude/rules/authorization.md`.

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

The same shape recurs across the `src/mcp` / `src/agent` boundary — two subtrees that don't reference
each other, needing one literal to agree on:

- `[McpServerTool(Name = ...)]` on a tool method in `Mcp.Tool` **declares** the protocol-level name.
- An agent's `ToolAllowList` in `Agent.Domain` **filters** against that same name.

```csharp
namespace Tnosc.EShop.Server.Shared.Catalog;

/// <summary>
/// MCP tool names shared by the Catalog bounded context's <c>[McpServerTool]</c> methods and any
/// agent that references a tool by name, so the protocol-level name declared on the tool and the
/// name an agent's allow-list filters against cannot drift apart.
/// </summary>
public static class McpToolNames
{
    /// <summary>Lists products from the catalogue.</summary>
    public const string ListProducts = "catalog_list_products";
}
```

Without this, the MCP SDK derives a tool's name from its C# method name when `Name` is left unset —
snake_cased, including the `Async` suffix — which is not a name anyone chose and not one an allow-list
should ever be written against.

## What does not belong here

- **Anything with behaviour.** `Shared` is constants and inert primitives. Business rules belong to
  the domain; orchestration to the application. `RolePermissions.For(role)` is a dictionary lookup
  that decides nothing — it is the boundary of what counts as inert here.
- **Types one project alone uses** — keep them next to their use.
- **Anything that would let two bounded contexts couple.** `Shared` is organised per context
  precisely so `Basket` cannot reach for a `Catalog` constant; contexts still communicate only
  through domain events and the outbox. `Authorization/` is the deliberate exception, and it holds
  only names — no context's rules, entities or behaviour.

## Conventions

- `public static class`, `public const string` members, XML docs on every member. Attribute arguments
  must be compile-time constants, so `const` — not `static readonly` — is required for anything a
  `[CacheTag(...)]` names.
- One file per concern, named for it (`CacheTags.cs`), under `Shared/<Context>/` — or under
  `Shared/Authorization/` for the cross-cutting authorization vocabulary.
- Same file header, explicit `using`s and file-scoped namespace as everywhere else.
