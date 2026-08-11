// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// Durable record of which idempotency keys a command handler has already answered, and with what.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must take part in the caller's ambient transaction, because that is what makes
/// the guarantee hold: the claim and the handler's own writes commit or roll back together, so a key
/// is never burned without its effect and an effect never lands without its key.
/// </para>
/// <para>
/// <see cref="ClaimAsync{TResponse}"/> must be conflict-tolerant rather than throwing on a duplicate
/// key — a failed insert would abort the transaction and poison every later statement on it. The
/// implementation is also what serialises concurrent duplicates: a second claim for a key an
/// uncommitted transaction already inserted has to <b>wait</b> for that transaction to settle, then
/// replay if it committed or acquire the key if it rolled back.
/// </para>
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    /// Claims <paramref name="key"/> for <paramref name="handlerName"/> on behalf of a handler that
    /// returns a response, reporting whether the caller must run the handler or replay a recorded
    /// response.
    /// </summary>
    /// <typeparam name="TResponse">The handler's response type.</typeparam>
    /// <param name="key">The caller-supplied idempotency key.</param>
    /// <param name="handlerName">The handler the key is scoped to, so two handlers cannot collide on one key.</param>
    /// <param name="requestHash">A hash of the request payload, used to detect a key reused with different content.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The claim outcome, carrying the recorded response on a replay.</returns>
    ValueTask<IdempotencyClaim<TResponse>> ClaimAsync<TResponse>(
        string key,
        string handlerName,
        string requestHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims <paramref name="key"/> for <paramref name="handlerName"/> on behalf of a handler that
    /// returns no response.
    /// </summary>
    /// <param name="key">The caller-supplied idempotency key.</param>
    /// <param name="handlerName">The handler the key is scoped to, so two handlers cannot collide on one key.</param>
    /// <param name="requestHash">A hash of the request payload, used to detect a key reused with different content.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The claim outcome; there is no response to carry.</returns>
    ValueTask<IdempotencyClaimStatus> ClaimAsync(
        string key,
        string handlerName,
        string requestHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the response a successful run produced, so a later duplicate of the same key replays
    /// it. Called only after a claim of <see cref="IdempotencyClaimStatus.Acquired"/>.
    /// </summary>
    /// <typeparam name="TResponse">The handler's response type.</typeparam>
    /// <param name="key">The idempotency key claimed earlier in this transaction.</param>
    /// <param name="handlerName">The handler the key is scoped to.</param>
    /// <param name="response">The response to record against the key.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    ValueTask CompleteAsync<TResponse>(
        string key,
        string handlerName,
        TResponse response,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a claimed key as successfully completed for a handler that returns no response.
    /// Called only after a claim of <see cref="IdempotencyClaimStatus.Acquired"/>.
    /// </summary>
    /// <param name="key">The idempotency key claimed earlier in this transaction.</param>
    /// <param name="handlerName">The handler the key is scoped to.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    ValueTask CompleteAsync(
        string key,
        string handlerName,
        CancellationToken cancellationToken = default);
}
