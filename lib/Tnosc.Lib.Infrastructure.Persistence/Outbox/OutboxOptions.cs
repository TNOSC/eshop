// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Configures how <see cref="IOutboxProcessor"/> and <see cref="OutboxBackgroundService{TContext}"/>
/// claim and retry outbox messages.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages claimed by a single processing batch. Defaults to 20.
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the interval between polling ticks. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the number of attempts after which a message is no longer claimed — dead-lettered
    /// by exclusion from the claim query. Defaults to 5.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the base duration used by the exponential backoff applied after a failed attempt.
    /// Defaults to 10 seconds.
    /// </summary>
    public TimeSpan BaseBackoff { get; set; } = TimeSpan.FromSeconds(10);
}
