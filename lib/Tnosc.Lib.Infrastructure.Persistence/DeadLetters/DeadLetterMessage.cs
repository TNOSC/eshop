// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Infrastructure.Persistence.DeadLetters;

/// <summary>
/// One domain event that one handler could not process, kept for inspection and replay after the
/// outbox exhausted its durable retries.
/// </summary>
/// <remarks>
/// The unit is <b>(event, handler)</b>, not (event). A single event fans out to many handlers and
/// they fail independently — recording the message alone would lose the only thing an operator
/// actually needs to know, which is <em>who</em> could not process it.
/// </remarks>
public sealed class DeadLetterMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterMessage"/> class.
    /// </summary>
    /// <param name="outboxMessageId">The outbox message this failure came from.</param>
    /// <param name="handler">The handler that failed, or <see langword="null"/> when delivery never reached one.</param>
    /// <param name="type">The domain event's durable contract name.</param>
    /// <param name="content">The serialized domain event, carried over verbatim so a replay sends exactly what failed.</param>
    /// <param name="occurredOnUtc">When the original event occurred.</param>
    /// <param name="deadLetteredOnUtc">When the message was moved here.</param>
    /// <param name="attempts">How many delivery attempts the outbox made before giving up.</param>
    /// <param name="error">The last error recorded for this handler.</param>
    public DeadLetterMessage(
        Guid outboxMessageId,
        string? handler,
        string type,
        string content,
        DateTime occurredOnUtc,
        DateTime deadLetteredOnUtc,
        int attempts,
        string error)
    {
        Id = Guid.CreateVersion7();
        OutboxMessageId = outboxMessageId;
        Handler = handler;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
        DeadLetteredOnUtc = deadLetteredOnUtc;
        Attempts = attempts;
        Error = error;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterMessage"/> class. For EF Core only.
    /// </summary>
    private DeadLetterMessage()
    {
        Type = string.Empty;
        Content = string.Empty;
        Error = string.Empty;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the outbox message this failure came from, for tracing.
    /// </summary>
    public Guid OutboxMessageId { get; private set; }

    /// <summary>
    /// Gets the durable name of the handler that failed.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when delivery never reached a handler at all — an unresolvable contract
    /// name, or a payload that would not deserialize. Such a message still belongs here rather than
    /// lingering in the outbox as a row nothing will ever claim again.
    /// </remarks>
    public string? Handler { get; private set; }

    /// <summary>
    /// Gets the domain event's durable contract name, as resolved by <c>DomainEventTypeRegistry</c>.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the serialized domain event.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Gets the UTC date and time the original event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>
    /// Gets the UTC date and time the message was moved to the dead-letter queue.
    /// </summary>
    public DateTime DeadLetteredOnUtc { get; private set; }

    /// <summary>
    /// Gets the number of delivery attempts the outbox made before giving up.
    /// </summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// Gets the last error recorded for this handler.
    /// </summary>
    public string Error { get; private set; }

    /// <summary>
    /// Gets the number of times a replay has been attempted.
    /// </summary>
    public int ReplayCount { get; private set; }

    /// <summary>
    /// Gets the UTC date and time of the most recent replay attempt, if any.
    /// </summary>
    public DateTime? LastReplayedOnUtc { get; private set; }

    /// <summary>
    /// Gets the UTC date and time a replay finally succeeded, if it has.
    /// </summary>
    /// <remarks>
    /// The row is kept rather than deleted, so the queue stays an audit trail of what broke and when
    /// it was recovered. Listings filter on this being <see langword="null"/>.
    /// </remarks>
    public DateTime? ReplayedOnUtc { get; private set; }

    /// <summary>
    /// Records a replay that succeeded, taking the message out of the pending queue.
    /// </summary>
    /// <param name="replayedOnUtc">When the replay succeeded.</param>
    public void MarkReplayed(DateTime replayedOnUtc)
    {
        ReplayCount++;
        LastReplayedOnUtc = replayedOnUtc;
        ReplayedOnUtc = replayedOnUtc;
    }

    /// <summary>
    /// Records a replay that failed again, leaving the message in the queue with the newer error.
    /// </summary>
    /// <param name="attemptedOnUtc">When the replay was attempted.</param>
    /// <param name="error">The error the replay produced.</param>
    public void MarkReplayFailed(DateTime attemptedOnUtc, string error)
    {
        ReplayCount++;
        LastReplayedOnUtc = attemptedOnUtc;
        Error = error;
    }
}
