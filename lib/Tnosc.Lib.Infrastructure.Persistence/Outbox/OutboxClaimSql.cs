// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Holds the Postgres-specific claim statement shared by every closed <see cref="OutboxProcessor{TContext}"/>.
/// </summary>
/// <remarks>
/// Kept in a non-generic type so the SQL text is compiled once and shared across all
/// <c>TContext</c> instantiations instead of being duplicated — and re-allocated — per closed
/// generic type (a <c>static</c> field on a generic type is per closed type, not shared).
/// </remarks>
internal static class OutboxClaimSql
{
    /// <summary>
    /// The claim statement. Parameters, in order: max attempts, current UTC time, batch size, and
    /// the instant the claim's lease expires.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately Postgres-specific — <c>FOR UPDATE SKIP LOCKED</c> is what lets several replicas
    /// poll the same table without ever double-claiming a row; the naive select-then-update pattern
    /// livelocks under concurrency. This is the one place in <c>lib/</c> that assumes Postgres; it
    /// stays contained here rather than leaking a Postgres dependency onto the rest of the
    /// persistence layer (which stays provider-agnostic and Npgsql-free).
    /// </para>
    /// <para>
    /// <c>SKIP LOCKED</c> alone is not enough, which is why the claim also takes a <b>lease</b> by
    /// pushing <c>next_attempt_on_utc</c> out. The row lock lasts only until this statement commits,
    /// and processing happens afterwards — so without a lease a second processor polling while the
    /// first is still working would re-claim the very same rows (they are still unprocessed and
    /// still due), delivering them twice and starving the rows nobody has claimed yet. The lease
    /// makes a claimed row invisible until it could plausibly have failed. Both terminal paths
    /// override it: success sets <c>processed_on_utc</c> and clears the lease, and
    /// <c>MarkFailed</c> replaces it with the real exponential backoff.
    /// </para>
    /// </remarks>
    public const string Text = $"""
        UPDATE {OutboxMessageConfiguration.SchemaName}.{OutboxMessageConfiguration.TableName}
        SET attempts = attempts + 1,
            next_attempt_on_utc = {"{3}"}
        WHERE id IN (
            SELECT id FROM {OutboxMessageConfiguration.SchemaName}.{OutboxMessageConfiguration.TableName}
            WHERE processed_on_utc IS NULL
              AND attempts < {"{0}"}
              AND (next_attempt_on_utc IS NULL OR next_attempt_on_utc <= {"{1}"})
            ORDER BY occurred_on_utc
            LIMIT {"{2}"}
            FOR UPDATE SKIP LOCKED)
        RETURNING *;
        """;
}
