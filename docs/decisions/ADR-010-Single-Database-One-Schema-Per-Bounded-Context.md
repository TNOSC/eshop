# ADR-010: One Postgres Database, One Schema Per Bounded Context

## Status

Accepted

## Date

2026-08-14

## Context

Five bounded contexts (Catalog, Identity, Basket, Ordering, Payment) each own their own data, and must not
reference each other directly. Domain events raised during a write need to be persisted transactionally
alongside the aggregate change (the transactional outbox — see ADR-011), which requires the event insert
and the aggregate insert to share one database transaction. A database engine also had to be chosen; the
outbox's competing-consumer processing needs safe row claiming under concurrency.

## Decision

PostgreSQL (via Npgsql, provisioned as an Aspire container resource) is the solution's only database. All
bounded contexts share **one physical database**, but each gets its **own Postgres schema** (plus a shared
`outbox` schema); tables and columns are `snake_case`, configured explicitly per `IEntityTypeConfiguration`.
Contexts remain logically isolated by folder structure and by never referencing each other's aggregates —
schema separation is the persistence-level enforcement of that same boundary.

## Rationale

- **`FOR UPDATE SKIP LOCKED` is what makes the outbox correct under concurrency.** Multiple outbox
  processor instances need to claim rows without blocking each other or double-processing; Postgres's
  `SKIP LOCKED` support is the specific capability this architecture depends on for that guarantee, and it
  drove the database choice directly.
- **One database, not one-per-context, because the write and the outbox insert must share a transaction.**
  `TransactionDecorator` injects a single `IUnitOfWork`, and the outbox row for a raised domain event must
  commit atomically with the aggregate write that raised it (ADR-011) — which requires both to go through
  the same `DbContext` and the same database. Splitting contexts into separate databases would break this
  atomicity guarantee (or require distributed transactions, which this architecture does not use).
- **Schema-per-context still enforces the "contexts don't share data" rule**, just at the database level
  instead of the process level — a context's tables are namespaced and not casually joinable from another
  context's queries, even though they're reachable in principle within one physical database.
- Alternative rejected: one database per bounded context — rejected specifically because it breaks the
  outbox's single-transaction atomicity between an aggregate write and its domain-event row, without a
  distributed-transaction mechanism this architecture deliberately avoids.

## Consequences

**Easier:**
- The transactional outbox is trivially atomic — the aggregate write and its event row commit or roll
  back together, by construction, because they are in the same transaction on the same database.
- One connection string, one Aspire Postgres resource, one set of migrations to run for local development.

**Harder:**
- Bounded-context isolation is a discipline enforced by code review and architecture tests (no
  cross-context aggregate references), not by a hard database-level barrier — a schema-qualified query
  reaching into another context's tables is possible and must be caught socially/by test, not by
  connectivity.
- A schema change in development may require dropping the shared Postgres data volume (`WithDataVolume()`
  on the Aspire resource), affecting every context's local data at once rather than just the one being
  changed.
