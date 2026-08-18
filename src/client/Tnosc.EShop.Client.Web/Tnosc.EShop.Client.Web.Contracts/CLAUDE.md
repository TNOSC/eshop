# Contracts — request/response DTOs and API routes shared by both hosts

Plain data only. This project has no logic of its own beyond `ApiRoutes`' route-building helpers; it
exists so the WASM `.Client` project and the server `.Web` host can share one set of wire types
instead of each declaring its own. Referenced by `.Client`; not referenced by `Server.*` — the two
sides of the wire are independent, matched only by convention (mirroring how `authorization.md`
describes permissions matching by spelling, not by shared assembly).

## Layout

```
<Context>/                     request/response records for that bounded context (Catalog, Identity, Basket, Ordering)
Routes/ApiRoutes.cs            every relative API path the client calls, as constants/builders — no leading slash
AssemblyReference.cs           stable Assembly handle for reflection-based scanning
```

## Rules

- **A type here is a DTO, never a domain type.** No behavior, no validation attributes for
  server-side rules (those live server-side); DataAnnotations here are only ever the client-side
  constraints a `<Name>ViewModel` in `.Client` copies onto itself for `ClientValidation.Validate`.
- **Routes have no leading slash.** `ApiRoutes` paths are combined with a `Uri` base address that
  already carries the BFF prefix (`/bff/` for WASM, service-discovery root for the interactive-server
  host) — a leading slash would discard that base path instead of appending to it. This is the same
  failure mode `EShopBffRoutes` warns about from the other side of the proxy.
- **One `ApiRoutes.<Context>` nested class per bounded context**, mirroring the server's own
  `<Context>Routes` constants (`.claude/rules/authorization.md` neighbors,
  `Server.Api/<Context>/<Context>Routes.cs`) — a route exists in exactly one place on each side of the
  wire, never inlined at a call site in `.Client`.
- Route builders that append query parameters use `CultureInfo.InvariantCulture` and
  `Uri.EscapeDataString` on any free-text parameter — see `ApiRoutes.Catalog.SearchProducts` for the
  pattern to copy.

## Checklist

- [ ] A new endpoint gets its relative path added to the matching `ApiRoutes.<Context>` class, not
      inlined in a `.Client` service.
- [ ] A new DTO carries no server-side business logic — it is shape only.
- [ ] A route builder with a free-text parameter escapes it with `Uri.EscapeDataString`.
