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

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Drives <see cref="IOutboxProcessor"/> on a fixed polling interval for as long as the host runs.
/// </summary>
/// <remarks>
/// <see cref="BackgroundService"/> instances are registered as singletons, but <see cref="IOutboxProcessor"/>
/// depends on a scoped <typeparamref name="TContext"/>. Capturing a scoped context in this singleton
/// would be the classic bug, so a fresh <see cref="IServiceScope"/> is created on every tick and
/// disposed before the next one starts.
/// </remarks>
/// <typeparam name="TContext">The write <see cref="DbContext"/> that owns the outbox table.</typeparam>
public sealed class OutboxBackgroundService<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxBackgroundService<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.PollingInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken: stoppingToken))
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IOutboxProcessor processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

                await processor.ProcessBatchAsync(cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host is shutting down; let the loop exit on the next WaitForNextTickAsync check.
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, message: "Outbox processing tick failed; will retry on the next poll.");
            }
        }
    }
}
