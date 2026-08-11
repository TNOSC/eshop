// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// One row of the dead-letter queue as an operator reads it: what failed, who failed to handle it,
/// and how many times recovery has been tried.
/// </summary>
/// <remarks>
/// The serialized payload is deliberately absent. A listing is for triage, and the payloads are
/// unbounded <c>jsonb</c>; anything needing the body reads the row directly.
/// </remarks>
/// <param name="Id">The dead-letter row identifier, used to replay or discard it.</param>
/// <param name="OutboxMessageId">The outbox message the failure came from.</param>
/// <param name="Handler">The handler that failed, or <see langword="null"/> when delivery never reached one.</param>
/// <param name="Type">The domain event's durable contract name.</param>
/// <param name="OccurredOnUtc">When the original event occurred.</param>
/// <param name="DeadLetteredOnUtc">When the message was moved to the queue.</param>
/// <param name="Attempts">How many delivery attempts the outbox made before giving up.</param>
/// <param name="Error">The most recent error recorded for this handler.</param>
/// <param name="ReplayCount">How many replays have been attempted.</param>
public sealed record DeadLetterSummary(
    Guid Id,
    Guid OutboxMessageId,
    string? Handler,
    string Type,
    DateTime OccurredOnUtc,
    DateTime DeadLetteredOnUtc,
    int Attempts,
    string Error,
    int ReplayCount);
