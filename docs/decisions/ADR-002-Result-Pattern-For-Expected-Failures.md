# ADR-002: Result Pattern For Expected Failures, Exceptions For The Unexpected

## Status

Accepted

## Date

2026-08-14

## Context

Domain and application code needs a way to signal validation failures, business-rule violations, and
not-found conditions back up to an endpoint, which must translate them into predictable HTTP responses.
Two conventional approaches exist: throwing exceptions for every failure case, or a `Result<T>` return
type. Infrastructure code also genuinely throws — `DbUpdateException`, `NpgsqlException` and similar are
real, unexpected technical failures that a domain-level `Result` was never designed to represent.

## Decision

Domain and Application layers use `Result<T>` (carrying an `ErrorType`) for every **expected** outcome:
validation failures, business-rule violations, not-found, conflict. Exceptions are reserved for
**unexpected** infrastructure failures. An `ExceptionHandlingDecorator` in the command/query pipeline
catches infrastructure exceptions and maps them into `Result.Failure(ErrorType.Unexpected)` before they
can reach an endpoint. Endpoints always consume a `Result<T>` and translate its `ErrorType` into the
matching HTTP status code (`Validation` → 400, `Unauthorized` → 401, `Forbidden` → 403, `NotFound` → 404,
`Conflict` → 409, `Failure`/`Unexpected` → 500, `Custom` → its own status).

## Rationale

- **Expected failures are values, not control flow.** A customer's cart failing to find a product, or an
  order total failing a business invariant, is a normal outcome the caller must handle — modeling it as a
  thrown exception makes the happy path indistinguishable from the failure path in a stack trace and
  forces `try/catch` into orchestration code that this codebase deliberately keeps branch-free (see
  ADR-005).
- **Exceptions stay meaningful.** Because `Result<T>` owns every expected case, an exception surfacing
  from Application or Domain code is unambiguously a real defect or a genuine infrastructure failure — not
  noise mixed in with business outcomes.
- **The hybrid keeps translation in one place.** Rather than every handler wrapping its own
  infrastructure call in `try/catch`, `ExceptionHandlingDecorator` is the single seam where
  `SqlException`/`DbUpdateException`-shaped failures become `Result.Failure(ErrorType.Unexpected)` — the
  Application layer never leaks a raw exception to an endpoint.
- Alternative rejected: exceptions for all failures (idiomatic in some .NET codebases) — rejected because
  it collapses "the customer typed an invalid SKU" and "the database connection dropped" into the same
  mechanism, and pushes exception-to-HTTP-status mapping logic into every endpoint instead of one
  `ErrorType`-driven translation.

## Consequences

**Easier:**
- Endpoints have one, uniform way to turn a failure into `ProblemDetails` — a single `ErrorType` switch.
- Handler and domain unit tests assert on a returned `Result<T>` value instead of asserting a thrown
  exception type and message.
- Validators return `Result` instead of throwing, keeping the same expected-outcome vocabulary end to end.

**Harder:**
- Every method that can fail expectedly must be authored to return `Result<T>` rather than `T` — this is
  more ceremony than "just throw" for a quick prototype.
- A new infrastructure exception type must be recognized by `ExceptionHandlingDecorator` (or its
  equivalent translation point) or it will propagate unmapped instead of becoming a clean 500.
