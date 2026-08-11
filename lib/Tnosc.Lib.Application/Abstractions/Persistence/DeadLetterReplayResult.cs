// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// The outcome of replaying one dead letter.
/// </summary>
public enum DeadLetterReplayResult
{
    /// <summary>
    /// The handler ran and succeeded. The row is stamped as replayed and leaves the pending queue.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// The handler ran and threw again. The row stays in the queue with its replay count raised and
    /// the newer error recorded — replaying something still broken is expected, not exceptional.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// No dead letter with that identifier exists, or it was already replayed.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// The row cannot be replayed at all: its contract name resolves to no known event type, its
    /// payload will not deserialize, or it names no handler. It needs a person, not another attempt.
    /// </summary>
    NotReplayable = 3,
}
