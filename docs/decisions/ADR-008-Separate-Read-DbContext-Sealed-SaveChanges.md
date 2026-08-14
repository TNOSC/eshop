# ADR-008: Separate Read DbContext, SaveChanges Sealed To Throw

## Status

Accepted

## Date

2026-08-14

## Context

Query handlers inject a `DbContext` directly (ADR-007) rather than going through a repository. Without
some other guardrail, nothing stops a future query handler from accidentally calling `SaveChangesAsync()`
on the context it was given — reintroducing a write on what is supposed to be a read-only path, and doing
so silently, since the read side has no repository boundary to catch it at review time.

## Decision

Reads use a dedicated `EShopReadDbContext`, distinct from the write side's `EShopWriteDbContext`. The read
context's `SaveChanges`/`SaveChangesAsync` are sealed to throw, so a write attempted through it fails hard
rather than silently succeeding.

## Rationale

- **A hard guarantee rather than a convention.** "Don't call `SaveChanges` on the read context" as an
  unenforced convention relies on every future contributor remembering it; overriding `SaveChanges` to
  throw turns a violation into an immediate, loud failure — a defense that does not depend on review
  catching it.
- **A seam for a future read replica.** Splitting the contexts now means the read context can later point
  at a physically separate read replica connection string without touching the write path at all — the
  separation already exists at the type level, so there is nothing to refactor when that need arrives.
- **Matches the CQRS split already made for query handlers vs. repositories** (ADR-006, ADR-007) —
  reinforcing at the persistence layer that reads and writes are two genuinely separate concerns, not just
  a naming convention on top of one shared context.
- Alternative rejected: one shared `DbContext` for both reads and writes — rejected because it gives every
  query handler the ability to accidentally mutate state, and forecloses the read-replica seam without a
  later breaking change.

## Consequences

**Easier:**
- A query handler cannot accidentally introduce a write — the failure is immediate and unambiguous
  (an exception at the call site) rather than a silent, unreviewed side effect.
- Read and write connection strings/behavior (e.g. execution strategy — see `idempotency.md`'s note that
  only the write context disables retry) can diverge cleanly per context.

**Harder:**
- Two `DbContext` types means two EF configurations to keep aligned when the schema changes, and every EF
  tool invocation must pass `--context` explicitly since only the write context has migrations (see
  `migrations.md`).
- A handler that needs both a read projection and a subsequent write (rare, but possible) cannot use one
  context for both — it must go through the read context for the projection and the repository/write
  context for the mutation.
