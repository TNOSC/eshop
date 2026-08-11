// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Configures how long idempotency and inbox claims are kept, and how
/// <see cref="IdempotencyCleanupBackgroundService{TContext}"/> collects the expired ones.
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>
    /// Gets or sets how long a claim is kept after it is made. Defaults to 24 hours.
    /// </summary>
    /// <remarks>
    /// This is the window in which a client's retry still replays the original answer. Shortening it
    /// bounds the table more tightly at the cost of turning a late retry back into a real execution;
    /// it never licenses reusing a key, because an uncollected expired row still blocks its key.
    /// </remarks>
    public TimeSpan Retention { get; set; } = TimeSpan.FromHours(value: 24);

    /// <summary>
    /// Gets or sets the interval between cleanup ticks. Defaults to 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(value: 1);

    /// <summary>
    /// Gets or sets the maximum number of rows deleted from each table by a single cleanup tick.
    /// Defaults to 500.
    /// </summary>
    public int BatchSize { get; set; } = 500;
}
