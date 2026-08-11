// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// Durable record of which domain event each handler has already processed — the inbox that turns
/// the outbox's at-least-once delivery into an at-most-once effect.
/// </summary>
/// <remarks>
/// The claim is written in the handler's own transaction, so a redelivery that arrives after a
/// successful run sees the committed claim and skips, while a redelivery that follows a crashed run
/// sees nothing (the claim rolled back with the partial work) and processes the event properly.
/// As with <see cref="IIdempotencyStore"/>, the claim must be conflict-tolerant rather than throwing,
/// because a failed insert aborts the transaction it is part of.
/// </remarks>
public interface IInboxStore
{
    /// <summary>
    /// Attempts to claim <paramref name="eventId"/> for <paramref name="handlerName"/>.
    /// </summary>
    /// <param name="eventId">The <see cref="Tnosc.Lib.Domain.IDomainEvent.Id"/> of the event being delivered.</param>
    /// <param name="handlerName">The handler the claim is scoped to, so each handler processes the event once.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// <see langword="true"/> when the claim was acquired and the caller must process the event;
    /// <see langword="false"/> when this handler already processed it and the caller must skip.
    /// </returns>
    ValueTask<bool> TryClaimAsync(
        Guid eventId,
        string handlerName,
        CancellationToken cancellationToken = default);
}
