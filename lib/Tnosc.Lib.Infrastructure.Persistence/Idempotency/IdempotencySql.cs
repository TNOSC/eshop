// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Holds the Postgres-specific idempotency and inbox statements shared by every closed
/// <see cref="IdempotencyStore{TContext}"/>, <see cref="InboxStore{TContext}"/> and
/// <see cref="IdempotencyCleanupBackgroundService{TContext}"/>.
/// </summary>
/// <remarks>
/// Kept in a non-generic type so each statement is compiled once and shared across all
/// <c>TContext</c> instantiations instead of being duplicated — and re-allocated — per closed
/// generic type (a <c>static</c> field on a generic type is per closed type, not shared).
/// </remarks>
internal static class IdempotencySql
{
    private const string RequestsTable = $"{IdempotencyRequestConfiguration.SchemaName}.{IdempotencyRequestConfiguration.TableName}";
    private const string ProcessedEventsTable = $"{ProcessedEventConfiguration.SchemaName}.{ProcessedEventConfiguration.TableName}";

    /// <summary>
    /// Claims an idempotency key. Parameters, in order: key, handler, request hash, created UTC,
    /// expires UTC. Affects one row when the key was free, none when it was already taken.
    /// </summary>
    /// <remarks>
    /// <c>ON CONFLICT DO NOTHING</c> rather than letting the unique violation throw: a failed insert
    /// aborts the surrounding Postgres transaction (<c>25P02</c>) and every later statement on it
    /// fails — including the handler's own writes, which share that transaction by design.
    /// <para>
    /// This statement is also what serialises concurrent duplicates. A second transaction inserting
    /// a key the first has inserted but not yet committed <b>blocks</b> on the row lock instead of
    /// seeing it, so it resumes only once the first has settled: it reports a conflict if the first
    /// committed, or takes the key if the first rolled back. That is why no "in progress" state is
    /// ever visible, and therefore why the table needs no status column.
    /// </para>
    /// </remarks>
    public const string Claim = $"""
        INSERT INTO {RequestsTable} (idempotency_key, handler, request_hash, created_on_utc, expires_on_utc)
        VALUES ({"{0}"}, {"{1}"}, {"{2}"}, {"{3}"}, {"{4}"})
        ON CONFLICT (idempotency_key, handler) DO NOTHING;
        """;

    /// <summary>
    /// Records the response of a successful run against a key claimed earlier in the same
    /// transaction. Parameters, in order: key, handler, response JSON, response type.
    /// </summary>
    /// <remarks>
    /// The explicit <c>CAST</c> is required: the parameter arrives as text and the column is
    /// <c>jsonb</c>, which Postgres will not coerce implicitly.
    /// </remarks>
    public const string Complete = $"""
        UPDATE {RequestsTable}
        SET response = CAST({"{2}"} AS jsonb), response_type = {"{3}"}
        WHERE idempotency_key = {"{0}"} AND handler = {"{1}"};
        """;

    /// <summary>
    /// Claims a domain event for one handler. Parameters, in order: event id, handler, processed
    /// UTC. Affects one row when this handler had not processed the event, none when it had.
    /// </summary>
    public const string ClaimEvent = $"""
        INSERT INTO {ProcessedEventsTable} (event_id, handler, processed_on_utc)
        VALUES ({"{0}"}, {"{1}"}, {"{2}"})
        ON CONFLICT (event_id, handler) DO NOTHING;
        """;

    /// <summary>
    /// Deletes a bounded batch of expired idempotency keys. Parameters, in order: current UTC, batch size.
    /// </summary>
    /// <remarks>
    /// Postgres has no <c>DELETE … LIMIT</c>, so the batch is selected by <c>ctid</c> in a subquery.
    /// Batching keeps each cleanup tick's lock footprint small rather than taking one long lock over
    /// a day's worth of rows.
    /// </remarks>
    public const string DeleteExpiredRequests = $"""
        DELETE FROM {RequestsTable}
        WHERE ctid IN (
            SELECT ctid FROM {RequestsTable}
            WHERE expires_on_utc <= {"{0}"}
            LIMIT {"{1}"});
        """;

    /// <summary>
    /// Deletes a bounded batch of inbox claims older than the retention cutoff. Parameters, in
    /// order: cutoff UTC, batch size.
    /// </summary>
    public const string DeleteExpiredProcessedEvents = $"""
        DELETE FROM {ProcessedEventsTable}
        WHERE ctid IN (
            SELECT ctid FROM {ProcessedEventsTable}
            WHERE processed_on_utc <= {"{0}"}
            LIMIT {"{1}"});
        """;
}
