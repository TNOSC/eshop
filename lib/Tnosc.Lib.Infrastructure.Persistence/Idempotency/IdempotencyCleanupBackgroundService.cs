// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Deletes expired idempotency keys and inbox claims on a fixed interval, so neither table grows
/// without bound.
/// </summary>
/// <remarks>
/// <see cref="BackgroundService"/> instances are singletons while <typeparamref name="TContext"/> is
/// scoped, so a fresh <see cref="IServiceScope"/> is created per tick and disposed before the next —
/// the same shape, and for the same reason, as <see cref="Outbox.OutboxBackgroundService{TContext}"/>.
/// </remarks>
/// <typeparam name="TContext">The write <see cref="DbContext"/> that owns both tables.</typeparam>
/// <param name="scopeFactory">Creates the per-tick scope the context is resolved from.</param>
/// <param name="options">Supplies the retention window, tick interval and per-tick batch size.</param>
/// <param name="timeProvider">Supplies the current UTC time used to compute the cutoff.</param>
/// <param name="logger">Records tick failures without stopping the loop.</param>
public sealed class IdempotencyCleanupBackgroundService<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<IdempotencyOptions> options,
    TimeProvider timeProvider,
    ILogger<IdempotencyCleanupBackgroundService<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.CleanupInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken: stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                TContext context = scope.ServiceProvider.GetRequiredService<TContext>();

                await CollectAsync(context: context, options: options.Value, timeProvider: timeProvider, cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host is shutting down; let the loop exit on the next WaitForNextTickAsync check.
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, message: "Idempotency cleanup tick failed; will retry on the next poll.");
            }
        }
    }

    /// <summary>
    /// Deletes one batch of expired rows from each table.
    /// </summary>
    /// <remarks>
    /// Exposed to the assembly so an integration test can run a single deterministic pass instead of
    /// waiting on the timer.
    /// </remarks>
    /// <param name="context">The write context to delete through.</param>
    /// <param name="options">The retention window and batch size to apply.</param>
    /// <param name="timeProvider">Supplies the current UTC time used to compute the cutoff.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of rows deleted across both tables.</returns>
    internal static async ValueTask<int> CollectAsync(
        TContext context,
        IdempotencyOptions options,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: context);
        ArgumentNullException.ThrowIfNull(argument: options);
        ArgumentNullException.ThrowIfNull(argument: timeProvider);

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        // Requests carry their own expiry, set when the key was claimed; inbox claims carry only the
        // instant they were made, so the retention window is applied to them here instead.
        int requests = await context.Database.ExecuteSqlRawAsync(
            sql: IdempotencySql.DeleteExpiredRequests,
            parameters: [now, options.BatchSize],
            cancellationToken: cancellationToken);

        int processedEvents = await context.Database.ExecuteSqlRawAsync(
            sql: IdempotencySql.DeleteExpiredProcessedEvents,
            parameters: [now.Subtract(value: options.Retention), options.BatchSize],
            cancellationToken: cancellationToken);

        return requests + processedEvents;
    }
}
