// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// One domain event a given handler has already processed — the inbox counterpart to the outbox,
/// turning at-least-once delivery into an at-most-once effect.
/// </summary>
/// <remarks>
/// Keyed on the event's own <see cref="Tnosc.Lib.Domain.IDomainEvent.Id"/> rather than the outbox row
/// id: the id is assigned by the aggregate that raised the event and serialized into the payload, so
/// it survives redelivery and stays stable even if the same logical event is enqueued again.
/// Rows are written by <see cref="InboxStore{TContext}"/> through raw, conflict-tolerant SQL, for
/// the same reason as <see cref="IdempotencyRequest"/>.
/// </remarks>
public sealed class ProcessedEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessedEvent"/> class.
    /// </summary>
    /// <remarks>
    /// EF Core materializes rows through this constructor by matching parameter names to properties.
    /// Production code never calls it — claims are written as raw SQL — but a test that needs a row
    /// in a specific state can.
    /// </remarks>
    /// <param name="eventId">The identifier of the domain event that was processed.</param>
    /// <param name="handler">The full type name of the handler that processed it.</param>
    /// <param name="processedOnUtc">The UTC date and time at which the claim was made.</param>
    public ProcessedEvent(Guid eventId, string handler, DateTime processedOnUtc)
    {
        EventId = eventId;
        Handler = handler;
        ProcessedOnUtc = processedOnUtc;
    }

    /// <summary>
    /// Gets the identifier of the domain event that was processed.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Gets the full type name of the handler that processed it, so each handler gets its own
    /// at-most-once guarantee for the same event.
    /// </summary>
    public string Handler { get; }

    /// <summary>
    /// Gets the UTC date and time at which the claim was made.
    /// </summary>
    public DateTime ProcessedOnUtc { get; }
}
