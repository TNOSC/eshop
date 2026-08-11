// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// The outcome of trying to claim an idempotency key for a handler.
/// </summary>
public enum IdempotencyClaimStatus
{
    /// <summary>
    /// The key was free and is now claimed by the caller, which must go on to run the handler.
    /// The claim is only durable once the surrounding transaction commits.
    /// </summary>
    Acquired = 0,

    /// <summary>
    /// The key was already used by a committed run of the same handler with the same payload. The
    /// caller must skip the handler and return the recorded response instead.
    /// </summary>
    Replay = 1,

    /// <summary>
    /// The key was already used by a committed run of the same handler with a <b>different</b>
    /// payload. Replaying the recorded response would answer a question the caller did not ask, so
    /// the caller must fail instead.
    /// </summary>
    PayloadMismatch = 2,

    /// <summary>
    /// The key was a replay, but the recorded response no longer fits the type the handler returns —
    /// the handler's response shape changed while the key was still live, or the recorded payload
    /// could not be read back. Replaying is impossible, so the caller must fail rather than guess.
    /// </summary>
    ResponseTypeMismatch = 3,
}
