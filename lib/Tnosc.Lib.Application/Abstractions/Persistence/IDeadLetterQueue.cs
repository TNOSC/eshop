// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// Inspection and recovery for domain events that a handler could not process, after the outbox
/// exhausted its durable retries.
/// </summary>
/// <remarks>
/// <para>
/// The queue's unit is <b>(event, handler)</b>. One event fanning out to several handlers fails per
/// handler, so recovery is per handler too: replay re-invokes only the handler named on the row, and
/// never disturbs the siblings that already succeeded.
/// </para>
/// <para>
/// There is no HTTP surface for this. Nothing in the solution wires authentication yet, and replay
/// and discard are operations nobody unauthenticated should reach.
/// </para>
/// </remarks>
public interface IDeadLetterQueue
{
    /// <summary>
    /// Lists dead letters awaiting attention, newest first. Rows recovered by an earlier replay are
    /// excluded — they remain as an audit trail, not as work.
    /// </summary>
    /// <param name="skip">How many rows to skip.</param>
    /// <param name="take">How many rows to return.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The pending dead letters.</returns>
    ValueTask<IReadOnlyList<DeadLetterSummary>> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-invokes the handler named on a dead letter with the event that defeated it.
    /// </summary>
    /// <remarks>
    /// A handler marked <c>[Idempotent]</c> is not blocked by its own inbox claim here: the claim was
    /// written in the transaction that then rolled back when the handler threw, so no claim exists to
    /// skip on. Its successful siblings' claims do still exist, which is why they are left alone.
    /// </remarks>
    /// <param name="deadLetterId">The dead letter to replay.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>Whether the handler succeeded, failed again, or could not be replayed.</returns>
    ValueTask<DeadLetterReplayResult> ReplayAsync(
        Guid deadLetterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dead letter outright, for a message that should never be processed.
    /// </summary>
    /// <param name="deadLetterId">The dead letter to discard.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns><see langword="true"/> when a row was deleted; <see langword="false"/> when none matched.</returns>
    ValueTask<bool> DiscardAsync(
        Guid deadLetterId,
        CancellationToken cancellationToken = default);
}
