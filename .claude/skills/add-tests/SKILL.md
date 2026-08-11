---
name: add-tests
description: Backfill missing tests for existing Tnosc.EShop code — domain unit tests, command-handler unit tests, and query-handler integration tests against real Postgres. Use when the user asks to add, improve, or backfill test coverage.
argument-hint: <target to cover, e.g. "UpdateProductPriceCommandHandler" or "the Money value object">
---

# Backfill Tests

Read the target first, list its distinct outcomes, then mirror the closest existing test class.
Full conventions: `tests/CLAUDE.md`. Templates: [../add-feature/references/tests.md](../add-feature/references/tests.md).

## Pick the right suite

| Target | Suite | Why |
|---|---|---|
| Aggregate, factory, value object, strategy | `Tests.Unit` | Pure business rules, no infrastructure |
| Command handler | `Tests.Unit` | NSubstitute over the domain repository contract |
| **Query handler** | `Tests.Integration` | Real SQL, real projections — a fake context proves nothing |
| `UnitOfWork`, outbox, auditing, EF conventions | `Tests.Integration` | Behaviour only a real database shows |
| A new rule you want enforced forever | `Tests.Architecture` | Mechanise it instead of relying on review |

Never unit-test a query handler, and never use the EF in-memory provider.

## Workflow

1. **Locate the target** and every distinct outcome: each `return` of an error, each guard inside the
   aggregate it delegates to, and the happy path.
2. **Check what already exists** in `tests/server/…Tests.Unit/<Context>/` and
   `…Tests.Integration/<Context>/` — extend the existing class rather than adding a parallel one.
3. **Write the tests**, one per outcome. Use the context's `*Faker` for data and the local
   `*TestFactory` for aggregates; never construct an aggregate by reaching into private state.
4. **Run** `dotnet test Tnosc.EShop.slnx` (Docker required for the integration suite) and fix
   failures before finishing.

## What to assert

**Command handlers — that they orchestrate and propagate, nothing more:**

- Happy path: the repository received the aggregate, `IUnitOfWork.SaveChangesAsync` was called once.
- Each failure path: the **same `ErrorType` and the same error code** the domain chose —
  `result.FirstError.Code.ShouldBe(expected: "Product.SkuAlreadyExists")`. A handler that
  reinterprets the domain's verdict is a bug, and this is the test that catches it.
- Every failure path: `_unitOfWork.DidNotReceive().SaveChangesAsync(...)`.
- Short-circuiting: a value-object failure never reaches the repository at all.

**Domain — the invariant, not the implementation:** each rejection returns its specific error code;
each successful transition mutates state, calls `IncrementVersion()`, and raises its domain event.

**Query handlers — the projection is correct against real SQL:** every DTO column maps to the right
read-model column, the not-found path, filtering/paging/ordering, and — for raw SQL — that joined
columns come back populated rather than null or defaulted.

## Reminders that bite

- Seed integration tests through **`UnitOfWork`**, not `WriteContext` directly, or you silently skip
  audit stamping and the domain-event-to-outbox conversion.
- Integration tests derive from the context's `*IntegrationTestBase` and run in
  `[Collection(nameof(PostgresCollection))]`; Respawn resets tables between tests, so never rely on
  data from a previous test.
- `TimeProvider` in integration tests is a manually-advanced `TestTimeProvider` — advance it rather
  than sleeping.
- Naming is `MethodOrScenario_Should_ExpectedOutcome_When_Condition`; classes are `public sealed`;
  Shouldly, not `Assert`; `// Arrange` / `// Act` / `// Assert`; name every argument.
- Warnings are errors in test projects too — analyzers still apply, only XML docs are exempt.

## Coverage gap worth flagging

If you find a rule enforced nowhere but a comment, or a handler whose failure path no test exercises,
say so explicitly rather than quietly adding a test that asserts current behaviour — current
behaviour may be the bug.
