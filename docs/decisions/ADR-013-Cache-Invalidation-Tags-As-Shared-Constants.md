# ADR-013: Cache Invalidation Tags As Shared Constants, Never Literals

## Status

Accepted

## Date

2026-08-14

## Context

HybridCache-backed query handlers populate the cache via `[Cacheable]` + `[CacheTag(...)]`, and command
handlers invalidate it via a matching `[CacheTag(...)]`. The two halves of this contract live in projects
that cannot see each other — query handlers in `Server.Infrastructure.Persistence`, command handlers in
`Server.Application` — and `CacheInvalidationDecorator` evicts purely by tag **string**.

## Decision

Every `[CacheTag(...)]` argument is a `const string` defined once in `Server.Shared/<Context>/CacheTags.cs`
— one class per bounded context, never shared across contexts. No string literal is ever passed to
`[CacheTag(...)]` directly. Start with a single context-wide tag and split into finer-grained tags only
once eviction is measurably too broad.

## Rationale

- **A spelling mismatch between the two halves fails silently, not loudly.** If the populating query
  handler and the invalidating command handler spell the tag differently, the build stays clean, both
  sides' isolated tests pass, and the query handler simply keeps serving a stale snapshot until its TTL
  expires — a bug that looks like "caching is broken" rather than "there's a typo," discovered far from
  where it was introduced.
- **`Server.Shared` is referenced by both projects**, so a `const` there turns the mismatch into a
  compile error: a wrong member name doesn't build. This is the same shape of problem, and the same fix,
  as `authorization.md`'s permission constants — a literal shared by two mutually-invisible halves of a
  contract needs a compile-time-checked home.
- **Context-scoped, not shared, classes.** A tag class per bounded context (rather than one solution-wide
  `CacheTags`) keeps contexts from being coupled through a shared enumeration of tags — exactly the kind
  of incidental coupling the bounded-context isolation rule (see ADR-010) exists to prevent.
- **Start broad, split only when proven necessary.** A tag per aggregate that every write handler carries
  anyway buys nothing; a single context-wide tag is simpler to reason about and correct until profiling
  shows the eviction radius is actually a problem.
- Alternative rejected: string literals at each `[CacheTag(...)]` call site — rejected as exactly the
  silent-drift failure mode above; alternative rejected: one solution-wide tag enum — rejected as coupling
  otherwise-isolated bounded contexts through a shared type.

## Consequences

**Easier:**
- A cache-tag typo is a compile error, not a runtime staleness bug discovered days later.
- `grep -rn 'CacheTag("' --include=*.cs src lib` returning nothing is a cheap, mechanical way to verify
  the rule holds across the whole solution.

**Harder:**
- Every new cached query and every write handler that invalidates it must remember to reference the
  shared constant rather than typing a tag inline — a habit that has to be taught, since nothing in the
  attribute's type signature forces a `const` from this particular class.
- Splitting a context-wide tag into finer-grained tags later is a coordinated change across every handler
  that currently shares it, not a local edit.
