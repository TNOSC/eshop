# ADR-004: No AutoMapper — Explicit DTO Projections

## Status

Accepted

## Date

2026-08-14

## Context

Query handlers project EF Core entities into read-side DTOs to avoid leaking domain entities across the
API boundary. AutoMapper is the conventional third-party choice for this projection step in many .NET
templates, typically via `IQueryable<TEntity>.ProjectTo<TDto>()` backed by reflection-based mapping
configuration.

## Decision

Every entity-to-DTO projection is a hand-written LINQ `Select(...)` expression, written directly in the
query handler. No AutoMapper (or any other reflection-based object-mapper) package is referenced anywhere
in the solution.

## Rationale

- **The projection is the query.** Query handlers already live in Infrastructure and hand-write raw SQL
  for complex multi-join queries (ADR-007); a `Select(...)` projection is a natural extension of "write the
  query, shape the result" rather than a separate mapping concern layered on top.
- **No reflection-based mapping surprises.** AutoMapper's configuration-by-convention (matching property
  names) can silently map the wrong property, or silently map nothing, when a DTO or entity is renamed —
  a failure mode that only appears at runtime. An explicit `Select` fails to compile instead.
- **Consistent with the "no third-party framework in the core layers" stance** taken for the mediator
  (ADR-001) and validation (ADR-003) — a mapping library is one more dependency and one more convention to
  learn, for a problem that EF Core's `IQueryable` projection already solves directly and efficiently
  (translated to SQL, rather than materializing full entities and mapping in memory).
- Alternative rejected: AutoMapper with `CreateMap<TEntity, TDto>()` profiles — rejected because the
  profile becomes an indirection between the entity and the DTO that must be kept in sync by convention,
  and it does not compose with hand-written raw SQL projections that the same query handlers also use for
  complex joins.

## Consequences

**Easier:**
- A DTO shape change is a compiler error at every call site that projects to it, not a silent
  runtime mismatch.
- Query handlers stay self-contained: the query and its projection are visible in one method, without
  jumping to a separate mapping profile file.

**Harder:**
- Every query handler repeats its own `Select(...)` projection — there is no shared mapping configuration
  to reuse across handlers that happen to project the same entity into similar shapes.
- Adding a field to a widely-used DTO means updating every handler that projects into it by hand, rather
  than updating one mapping profile.
