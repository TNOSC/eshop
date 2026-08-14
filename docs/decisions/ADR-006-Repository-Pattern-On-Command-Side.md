# ADR-006: Repository Pattern On The Command Side, Contracts Live In Domain

## Status

Accepted

## Date

2026-08-14

## Context

Command handlers need to persist aggregates without depending on EF Core directly, and some invariants —
SKU uniqueness, for example — require checking existing state before an aggregate can be constructed. The
repository contract has to live somewhere: Application (the conventional Clean Architecture placement) or
Domain.

## Decision

The command side uses the Repository pattern (`IOrderRepository.AddAsync`, etc.) expressing business
intent rather than CRUD, with EF Core hidden entirely behind the contract. The repository **interfaces**
are defined in `Server.Domain`, not `Server.Application`; only their EF Core implementations live in
Infrastructure.

## Rationale

- **Uniqueness invariants are business rules the domain must enforce**, and a domain factory (e.g. "create
  this product only if the SKU is unique") needs repository access to check that before the aggregate can
  even be constructed. If the contract lived in Application, the domain factory could not depend on it —
  `Server.Domain` must not reference `Server.Application` (dependencies point inward only).
- **Keeps the dependency direction correct without an exception carved out for this one case.** Domain
  defines what it needs (`IOrderRepository`); Infrastructure provides it; Application only consumes it to
  hand a fully-formed aggregate to `AddAsync`. No layer references outward.
- **Expresses intent, not persistence mechanics.** `AddOrder`/`UpdateOrderStatus`-shaped methods keep EF
  Core's `DbSet`/`Add`/`Update` vocabulary out of Application entirely, consistent with Infrastructure
  being "dumb and policy-free" — the repository's job is to persist what the domain already decided, not
  to decide anything itself.
- Alternative rejected: repository interfaces in Application (the more common Clean Architecture default)
  — rejected specifically because it would block domain factories from depending on them for invariant
  checks, forcing that logic either into Application (violating ADR-005) or into a separate mechanism.

## Consequences

**Easier:**
- A domain factory can enforce a uniqueness or existence invariant directly, by depending on a repository
  interface that lives in its own layer.
- Application handlers stay thin: construct the command's inputs, call the domain, call
  `repository.AddAsync(...)`, return.

**Harder:**
- Repository contracts sit in Domain, which is one more thing a Domain-layer reader has to hold in mind
  as "not itself business logic" — it is an infrastructure seam that happens to be declared there for
  dependency-direction reasons.
- Query-side reads deliberately bypass this pattern entirely (ADR-007), so "repository" on this codebase
  means specifically "command-side, write-only, business-intent contract" — a repository is never used for
  a read.
