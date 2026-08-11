// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tnosc.Lib.Application.Abstractions.Persistence;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Postgres-backed <see cref="IIdempotencyStore"/> writing through <typeparamref name="TContext"/>,
/// so every claim shares whichever transaction the calling handler is running in.
/// </summary>
/// <typeparam name="TContext">The write <see cref="DbContext"/> that owns the idempotency table.</typeparam>
/// <param name="context">The write context whose connection and transaction the claim joins.</param>
/// <param name="timeProvider">Supplies the current UTC time for claim and expiry stamping.</param>
/// <param name="options">Supplies the retention window applied to a new claim.</param>
/// <param name="logger">Records the states that should never occur but must not be silent.</param>
internal sealed class IdempotencyStore<TContext>(
    TContext context,
    TimeProvider timeProvider,
    IOptions<IdempotencyOptions> options,
    ILogger<IdempotencyStore<TContext>> logger)
    : IIdempotencyStore
    where TContext : DbContext
{
    /// <inheritdoc />
    public async ValueTask<IdempotencyClaim<TResponse>> ClaimAsync<TResponse>(
        string key,
        string handlerName,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        if (await TryInsertAsync(key: key, handlerName: handlerName, requestHash: requestHash, cancellationToken: cancellationToken))
        {
            return IdempotencyClaim<TResponse>.Acquired();
        }

        IdempotencyRequest? existing = await FindAsync(key: key, handlerName: handlerName, cancellationToken: cancellationToken);

        if (existing is null)
        {
            LogVanished(key: key, handlerName: handlerName);

            return IdempotencyClaim<TResponse>.ResponseTypeMismatch();
        }

        if (!string.Equals(a: existing.RequestHash, b: requestHash, comparisonType: StringComparison.Ordinal))
        {
            return IdempotencyClaim<TResponse>.PayloadMismatch();
        }

        return Replay<TResponse>(existing: existing);
    }

    /// <inheritdoc />
    public async ValueTask<IdempotencyClaimStatus> ClaimAsync(
        string key,
        string handlerName,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        if (await TryInsertAsync(key: key, handlerName: handlerName, requestHash: requestHash, cancellationToken: cancellationToken))
        {
            return IdempotencyClaimStatus.Acquired;
        }

        IdempotencyRequest? existing = await FindAsync(key: key, handlerName: handlerName, cancellationToken: cancellationToken);

        if (existing is null)
        {
            LogVanished(key: key, handlerName: handlerName);

            return IdempotencyClaimStatus.ResponseTypeMismatch;
        }

        return string.Equals(a: existing.RequestHash, b: requestHash, comparisonType: StringComparison.Ordinal)
            ? IdempotencyClaimStatus.Replay
            : IdempotencyClaimStatus.PayloadMismatch;
    }

    /// <inheritdoc />
    public async ValueTask CompleteAsync<TResponse>(
        string key,
        string handlerName,
        TResponse response,
        CancellationToken cancellationToken = default)
    {
        string payload = JsonSerializer.Serialize(value: response, options: IdempotencySerialization.Options);

        await context.Database.ExecuteSqlRawAsync(
            sql: IdempotencySql.Complete,
            parameters: [key, handlerName, payload, ResponseTypeNameOf<TResponse>()],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask CompleteAsync(
        string key,
        string handlerName,
        CancellationToken cancellationToken = default) =>
        await context.Database.ExecuteSqlRawAsync(
            sql: IdempotencySql.Complete,
            parameters: [key, handlerName, DBNull.Value, DBNull.Value],
            cancellationToken: cancellationToken);

    /// <summary>
    /// Inserts the claim row, tolerating the conflict that means someone already holds the key.
    /// </summary>
    /// <param name="key">The caller-supplied idempotency key.</param>
    /// <param name="handlerName">The handler the key is scoped to.</param>
    /// <param name="requestHash">The hash of the request payload claiming the key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the row was inserted and the key is now held by the caller.</returns>
    private async ValueTask<bool> TryInsertAsync(string key, string handlerName, string requestHash, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        int inserted = await context.Database.ExecuteSqlRawAsync(
            sql: IdempotencySql.Claim,
            parameters: [key, handlerName, requestHash, now, now.Add(value: options.Value.Retention)],
            cancellationToken: cancellationToken);

        return inserted == 1;
    }

    /// <summary>
    /// Reads back the row that won the key.
    /// </summary>
    /// <param name="key">The caller-supplied idempotency key.</param>
    /// <param name="handlerName">The handler the key is scoped to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async ValueTask<IdempotencyRequest?> FindAsync(string key, string handlerName, CancellationToken cancellationToken) =>
        await context.Set<IdempotencyRequest>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                predicate: r => r.Key == key && r.Handler == handlerName,
                cancellationToken: cancellationToken);

    /// <summary>
    /// Reads the recorded response back as <typeparamref name="TResponse"/>, refusing the replay
    /// rather than guessing when the recorded shape no longer matches.
    /// </summary>
    /// <typeparam name="TResponse">The response type the handler returns today.</typeparam>
    /// <param name="existing">The row that originally claimed the key.</param>
    private IdempotencyClaim<TResponse> Replay<TResponse>(IdempotencyRequest existing)
    {
        string expectedType = ResponseTypeNameOf<TResponse>();

        if (existing.Response is null || !string.Equals(a: existing.ResponseType, b: expectedType, comparisonType: StringComparison.Ordinal))
        {
            logger.LogError(
                message: "Idempotency key {Key} for {Handler} recorded response type {RecordedType}, which cannot be replayed as {ExpectedType}.",
                existing.Key,
                existing.Handler,
                existing.ResponseType,
                expectedType);

            return IdempotencyClaim<TResponse>.ResponseTypeMismatch();
        }

        TResponse? response = JsonSerializer.Deserialize<TResponse>(json: existing.Response, options: IdempotencySerialization.Options);

        return response is null
            ? IdempotencyClaim<TResponse>.ResponseTypeMismatch()
            : IdempotencyClaim<TResponse>.Replay(response: response);
    }

    /// <summary>
    /// Records that the row backing a conflicting key was gone by the time it was read — which the
    /// design does not allow, since only expired rows are ever collected and an expired key's holder
    /// returned long ago. Logged rather than swallowed so the impossible state is visible.
    /// </summary>
    /// <param name="key">The caller-supplied idempotency key.</param>
    /// <param name="handlerName">The handler the key is scoped to.</param>
    private void LogVanished(string key, string handlerName) =>
        logger.LogError(
            message: "Idempotency key {Key} for {Handler} conflicted on insert but no row could be read back.",
            key,
            handlerName);

    /// <summary>
    /// Gets the durable name a response is recorded under, used to refuse a replay whose shape has
    /// since changed.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    private static string ResponseTypeNameOf<TResponse>() =>
        typeof(TResponse).FullName ?? typeof(TResponse).Name;
}
