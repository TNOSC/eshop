// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Every way a command marked <see cref="IdempotentAttribute"/> can be rejected before its handler
/// ever runs.
/// </summary>
public static class IdempotencyErrors
{
    /// <summary>
    /// Gets the error returned when the caller supplied no idempotency key for a handler that
    /// requires one.
    /// </summary>
    /// <remarks>
    /// Deliberately a hard failure rather than a silent pass-through: the handler's author opted
    /// into <see cref="IdempotentAttribute"/>, so quietly running unguarded would make the guarantee
    /// a lie exactly when a retrying client depends on it.
    /// </remarks>
    public static Error KeyMissing => Error.Validation(
        code: "Idempotency.KeyMissing",
        description: "An Idempotency-Key is required for this request.");

    /// <summary>
    /// Gets the error returned when a key that already answered one request is presented again with
    /// a different payload.
    /// </summary>
    public static Error KeyReuse => Error.Conflict(
        code: "Idempotency.KeyReuse",
        description: "This Idempotency-Key was already used for a request with different content.");

    /// <summary>
    /// Gets the error returned when a recorded response cannot be replayed as the type the handler
    /// now returns — the handler's response shape changed while the key was still live.
    /// </summary>
    public static Error ResponseTypeMismatch => Error.Failure(
        code: "Idempotency.ResponseTypeMismatch",
        description: "The response recorded for this Idempotency-Key can no longer be replayed.");
}
