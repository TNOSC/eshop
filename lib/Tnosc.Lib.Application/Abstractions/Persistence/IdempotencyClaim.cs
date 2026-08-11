// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// The outcome of claiming an idempotency key for a handler that returns a response, carrying the
/// recorded response when the claim turned out to be a replay.
/// </summary>
/// <typeparam name="TResponse">The handler's response type.</typeparam>
/// <param name="Status">Whether the key was acquired, is a replay, or was reused with a different payload.</param>
/// <param name="Response">
/// The response recorded by the original run, set only when <paramref name="Status"/> is
/// <see cref="IdempotencyClaimStatus.Replay"/>; <see langword="default"/> otherwise.
/// </param>
public readonly record struct IdempotencyClaim<TResponse>(
    IdempotencyClaimStatus Status,
    TResponse? Response)
{
    /// <summary>
    /// Creates a claim for a key that was free, so the handler still has to run.
    /// </summary>
    public static IdempotencyClaim<TResponse> Acquired() =>
        new(Status: IdempotencyClaimStatus.Acquired, Response: default);

    /// <summary>
    /// Creates a claim for a key whose original run already completed, carrying its response.
    /// </summary>
    /// <param name="response">The response recorded by the original run.</param>
    public static IdempotencyClaim<TResponse> Replay(TResponse? response) =>
        new(Status: IdempotencyClaimStatus.Replay, Response: response);

    /// <summary>
    /// Creates a claim for a key that was reused with a payload other than the original's.
    /// </summary>
    public static IdempotencyClaim<TResponse> PayloadMismatch() =>
        new(Status: IdempotencyClaimStatus.PayloadMismatch, Response: default);

    /// <summary>
    /// Creates a claim for a replay whose recorded response cannot be read back as
    /// <typeparamref name="TResponse"/>.
    /// </summary>
    public static IdempotencyClaim<TResponse> ResponseTypeMismatch() =>
        new(Status: IdempotencyClaimStatus.ResponseTypeMismatch, Response: default);
}
