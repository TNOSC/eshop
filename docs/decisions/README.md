# Architectural Decision Records

This directory contains Architecture Decision Records (ADRs) for Tnosc.EShop.

An ADR captures a significant architectural decision: the context that led to it, the decision itself, the
rationale behind it, and its consequences. Use [ADR-000-template.md](ADR-000-template.md) when adding a
new record.

| ADR | Title | Date | Status |
|---|---|---|---|
| [ADR-001](ADR-001-No-Mediator-Library-Custom-CQRS-Pipeline.md) | No Mediator Library — Custom CQRS Pipeline | 2026-08-14 | Accepted |
| [ADR-002](ADR-002-Result-Pattern-For-Expected-Failures.md) | Result Pattern For Expected Failures, Exceptions For The Unexpected | 2026-08-14 | Accepted |
| [ADR-003](ADR-003-No-FluentValidation-Hand-Rolled-Validators.md) | No FluentValidation — Hand-Rolled Validators Returning Result | 2026-08-14 | Accepted |
| [ADR-004](ADR-004-No-AutoMapper-Explicit-Dto-Projections.md) | No AutoMapper — Explicit DTO Projections | 2026-08-14 | Accepted |
| [ADR-005](ADR-005-Rich-Domain-Model-Owns-Business-Decisions.md) | Rich Domain Model Owns All Business Decisions | 2026-08-14 | Accepted |
| [ADR-006](ADR-006-Repository-Pattern-On-Command-Side.md) | Repository Pattern On The Command Side, Contracts Live In Domain | 2026-08-14 | Accepted |
| [ADR-007](ADR-007-Query-Handlers-In-Infrastructure-With-Direct-DbContext.md) | Query Handlers Live In Infrastructure With Direct DbContext Access | 2026-08-14 | Accepted |
| [ADR-008](ADR-008-Separate-Read-DbContext-Sealed-SaveChanges.md) | Separate Read DbContext, SaveChanges Sealed To Throw | 2026-08-14 | Accepted |
| [ADR-009](ADR-009-Cross-Cutting-Concerns-Via-Decorators.md) | Cross-Cutting Concerns Via Scrutor Decorators, Not A Mediator Pipeline | 2026-08-14 | Accepted |
| [ADR-010](ADR-010-Single-Database-One-Schema-Per-Bounded-Context.md) | One Postgres Database, One Schema Per Bounded Context | 2026-08-14 | Accepted |
| [ADR-011](ADR-011-Transactional-Outbox-For-Domain-Events.md) | Transactional Outbox With Immutable Domain-Event Wire Contracts | 2026-08-14 | Accepted |
| [ADR-012](ADR-012-Idempotency-Claimed-In-Handler-Transaction.md) | Idempotency Claimed In The Handler's Own Transaction | 2026-08-14 | Accepted |
| [ADR-013](ADR-013-Cache-Invalidation-Tags-As-Shared-Constants.md) | Cache Invalidation Tags As Shared Constants, Never Literals | 2026-08-14 | Accepted |
| [ADR-014](ADR-014-Configuration-Bound-To-Narrow-Options-Classes.md) | Configuration Bound To Narrow Options Classes, Read Once At Composition Root | 2026-08-14 | Accepted |
| [ADR-015](ADR-015-Authorization-Roles-In-Keycloak-Permissions-In-Code.md) | Authorization — Roles In Keycloak, Permissions As Code Constants | 2026-08-14 | Accepted |
| [ADR-016](ADR-016-Architecture-Rules-Mechanised-With-Tests.md) | Architecture Rules Mechanised With NetArchTest + Roslyn | 2026-08-14 | Accepted |
