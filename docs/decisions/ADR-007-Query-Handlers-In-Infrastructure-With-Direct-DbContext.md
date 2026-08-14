# ADR-007: Query Handlers Live In Infrastructure With Direct DbContext Access

## Status

Accepted

## Date

2026-08-14

## Context

The command side goes through repository abstractions to keep EF Core out of Application (ADR-006). Reads
have no invariants to protect and no aggregate to reconstitute — they exist purely to shape data for a
caller. The question is whether query handlers should follow the same repository-abstraction discipline as
commands, or use EF Core directly.

## Decision

Query handlers are **not** abstracted behind a repository. They live in `Server.Infrastructure.Persistence`
and inject the read `DbContext` (`IAppDbContext`/`EShopReadDbContext`) directly, project straight into DTOs
with `Select(...)`, and use `AsNoTracking()` throughout. Complex multi-table joins with partial column
projections are written as raw SQL rather than forced through LINQ. The read context defaults every query
to `AsNoTracking()`.

## Rationale

- **A query has no business decision to protect.** ADR-006's repository pattern exists because a command
  needs an abstraction the domain can depend on for invariant enforcement; a query only needs to fetch and
  shape data, so an abstraction layer over it would add indirection with nothing to protect behind it.
- **`IAppDbContext` in Infrastructure, not Application, keeps EF Core out of Application** entirely —
  `LayerDependencyTests.Only_Persistence_Assemblies_Should_Depend_On_EfCore` enforces this — while still
  letting query handlers use EF Core's query capabilities (translated SQL, `AsNoTracking`, projection)
  directly rather than through a repository facade that would just re-expose `IQueryable` anyway.
- **Raw SQL for genuinely complex joins is a pragmatic escape hatch** from LINQ's translation limits and
  over-fetching tendencies on wide multi-table queries, kept local to the one handler that needs it instead
  of becoming a general pattern.
- Alternative rejected: a query-side repository mirroring the command side — rejected as needless
  indirection; a `IQueryable`-returning repository method offers nothing a direct `DbContext` injection
  doesn't, and CQRS's whole premise is that reads and writes are allowed to diverge in shape.

## Consequences

**Easier:**
- Query handlers are fast to write and read — the query is the handler, no repository interface/
  implementation pair to add for a new read.
- `AsNoTracking()` by default on the read context removes a whole class of "why is EF tracking this
  read-only entity" bugs.

**Harder:**
- Query handlers must be tested against real Postgres (integration tests, ADR-016), not mocked, since
  there is no abstraction seam to substitute — this is a deliberate trade-off recorded in the design doc's
  Testing Rules ("Integration tests must be applied to QueryHandlers").
- Raw SQL queries lose compile-time checking against the schema; a column rename requires manually
  finding and updating any raw SQL that references it.
