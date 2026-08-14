# ADR-012: Idempotency Claimed In The Handler's Own Transaction

## Status

Accepted

## Date

2026-08-14

## Context

At-least-once outbox delivery (ADR-011) means any domain-event handler, and any command re-sent by a
retrying client with an `Idempotency-Key`, can be invoked more than once for the same logical operation.
Preventing a duplicate effect needs a "have I already done this" claim. The claim and the handler's own
writes can be recorded in one transaction, or in separate transactions coordinated by a status column
(`Pending`/`InProgress`/`Done`).

## Decision

A handler marked `[Idempotent]` claims its key (the caller's `Idempotency-Key` for commands,
`IDomainEvent.Id` for domain events) in the **same transaction** as its own writes — via
`IdempotencyDecorator`, registered innermost in the command pipeline (inside `TransactionDecorator`,
directly around the handler) and as the only decorator on the domain-event pipeline. There is no status
column: the claim is a plain `INSERT ... ON CONFLICT`, relying on Postgres's READ COMMITTED isolation to
make a concurrent duplicate block on the first transaction's uncommitted row rather than observe a
half-finished state.

## Rationale

- **A two-phase claim always has a data-losing window.** Claim-then-run: a crash between them burns the
  key with no effect, so the client's retry is told "already done" for work that never happened. Run-then-
  claim: a crash between them leaves an effect with no recorded key, so the retry runs it again — precisely
  the duplicate the attribute exists to prevent. Separate transactions with a status column have both
  windows, plus a reconciliation job needed to decide what an abandoned `InProgress` row means. One
  transaction has neither window: the claim and the effect commit together or roll back together.
- **No status column is needed, not just no status column was added.** Under READ COMMITTED, a concurrent
  duplicate's `INSERT ... ON CONFLICT` blocks on the first transaction's uncommitted row and resumes only
  once it settles — replaying if it committed, taking the key if it rolled back. "In progress" is never an
  observable state to a second transaction, so there is nothing for a status column to represent.
- **Retry stays outside the idempotency claim, deliberately.** Each retry attempt needs its own
  transaction; a retry *inside* the claim would run on a transaction Postgres has already aborted
  (`25P02`). Keeping `Retry` outside means a failed attempt discards the claim along with the handler's
  partial work, so the next attempt cleanly re-claims and re-runs — a retried `[Idempotent]` handler still
  produces exactly one effect.
- Alternative rejected: claim recorded in a separate transaction (before or after the handler runs) —
  rejected for the data-loss windows above; alternative rejected: a status column with a background
  reconciliation job for abandoned `InProgress` rows — rejected as solving a problem (representing "in
  progress") that the single-transaction design makes unnecessary to represent at all.

## Consequences

**Easier:**
- The handler itself stays unaware of idempotency entirely — no key parameter, no dedupe branch (which
  `NoBusinessBranchingTests` would reject as business branching anyway, per ADR-005).
- A failed command frees its own key (the transaction rolls back), so the same client-supplied
  `Idempotency-Key` can be legitimately retried after a genuine failure.

**Harder:**
- The write `DbContext` cannot use EF Core's retrying execution strategy, since EF Core refuses a
  user-initiated transaction while one is configured (`DisableRetry = true` on the write context) — retry
  is owned explicitly by `RetryDecorator`/`[Retry(n)]` instead, which must stay outside the idempotency
  claim in the pipeline order.
- Any scope holding an `IUnitOfWork` must be disposed asynchronously (`CreateAsyncScope()`), since
  `UnitOfWork<TContext>` only implements `IAsyncDisposable` — getting this wrong surfaces as an event
  appearing to fail *after* its handler already succeeded.
