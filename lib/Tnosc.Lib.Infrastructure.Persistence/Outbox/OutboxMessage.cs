// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Text;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Represents the outbox message.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxMessage"/> class.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="content">The content.</param>
    public OutboxMessage(string type, string content)
    {
        Id = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
        Content = content;
        Type = type;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the occurred on date and time.
    /// </summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>
    /// Gets the type.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the content.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Gets the processed on date and time, if it exists.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; private set; }

    /// <summary>
    /// Gets the error, if it exists.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Gets the number of processing attempts made for this message.
    /// </summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// Gets the earliest UTC date and time at which the message may be attempted again, if it exists.
    /// </summary>
    public DateTime? NextAttemptOnUtc { get; private set; }

    /// <summary>
    /// Marks the item as processed at the specified UTC date and time, and clears any existing error state.
    /// </summary>
    /// <param name="processedOnUtc">The date and time, in UTC, when the item was processed.</param>
    public void MarkProcessed(DateTime processedOnUtc)
    {

        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    /// <summary>
    /// Marks a processing attempt as failed, records the specified error message, increments
    /// <see cref="Attempts"/>, and schedules the next attempt.
    /// </summary>
    /// <param name="error">The error message that describes the reason for the failure. Cannot be null.</param>
    /// <param name="nextAttemptOnUtc">The UTC date and time at which the next attempt may be made.</param>
    public void MarkFailed(string error, DateTime nextAttemptOnUtc)
    {
        Error = error;
        ProcessedOnUtc = null;
        Attempts++;
        NextAttemptOnUtc = nextAttemptOnUtc;
    }
}
