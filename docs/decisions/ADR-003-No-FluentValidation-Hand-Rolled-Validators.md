# ADR-003: No FluentValidation — Hand-Rolled Validators Returning Result

## Status

Accepted

## Date

2026-08-14

## Context

Commands need structural validation (required fields, formats, DTO shape) before a handler runs, distinct
from business-rule validation (order total > 0, email uniqueness), which belongs to the domain. FluentValidation
is the conventional third-party choice for the former in most .NET Clean Architecture templates, typically
wired in as a MediatR pipeline behavior that throws a `ValidationException`.

## Decision

Structural/DTO-shape validation is expressed through a hand-written `IValidator<T>` contract that returns
`Result` (never throws), run by a `Validation` decorator in the command pipeline. No FluentValidation
package (or any third-party validation library) is referenced by `Server.Application`.

## Rationale

- **Keeps Application dependency-free of third-party frameworks**, matching the same reasoning applied to
  the mediator (ADR-001) and the mapper (ADR-004): the Application layer's job is orchestration, and every
  third-party package it takes on is a package `LayerDependencyTests` must now tolerate and every
  contributor must now know, for a problem ("is this string non-empty, is this format valid") that a
  handful of hand-written classes solve without a fluent DSL to learn.
- **Consistent failure vocabulary.** FluentValidation's idiomatic failure mode is a thrown
  `ValidationException`; this codebase's failure vocabulary is `Result<T>` end to end (ADR-002). Returning
  `Result` from `IValidator<T>` avoids mixing exception-based and Result-based failure signaling in the
  same pipeline.
- **The split with domain validation stays a design decision, not a library boundary.** "Validators check
  structure, domain factories/entities check business rules" is easier to keep sharp when both are plain
  C#, reviewed the same way, rather than one being governed by a separate library's conventions.
- Alternative rejected: FluentValidation with a MediatR `ValidationBehavior` — rejected together with
  ADR-001, since it depends on the mediator pipeline this project does not use, and its exception-based
  failure mode conflicts with ADR-002.

## Consequences

**Easier:**
- One failure vocabulary (`Result`/`ErrorType`) for both structural and business validation.
- No FluentValidation-specific test helpers or `TestValidator` patterns to learn — a validator test is a
  plain unit test against `IValidator<T>.ValidateAsync(...)`.

**Harder:**
- No fluent rule-builder DSL — each validator is hand-written, which is more typing for large DTOs with
  many structural rules than FluentValidation's `RuleFor(...)` chains would be.
- No built-in cross-field or conditional-rule composition helpers; anything beyond simple field checks is
  written by hand rather than composed from a library's rule set.
