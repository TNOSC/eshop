// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Tnosc.Lib.Infrastructure.Persistence.Publishers;

namespace Tnosc.Lib.Infrastructure.Persistence.DeadLetters;

/// <summary>
/// Postgres-backed <see cref="IDeadLetterQueue"/> over <typeparamref name="TContext"/>.
/// </summary>
/// <typeparam name="TContext">The write <see cref="DbContext"/> that owns the dead-letter table.</typeparam>
/// <param name="context">The write context the queue reads and writes through.</param>
/// <param name="registry">Resolves a stored contract name back to its CLR event type.</param>
/// <param name="publisher">Delivers the replayed event to the one handler that failed.</param>
/// <param name="timeProvider">Supplies the current UTC time for replay stamping.</param>
/// <param name="logger">Records replay outcomes.</param>
internal sealed class DeadLetterQueue<TContext>(
    TContext context,
    IDomainEventTypeRegistry registry,
    IDomainEventsPublisher publisher,
    TimeProvider timeProvider,
    ILogger<DeadLetterQueue<TContext>> logger)
    : IDeadLetterQueue
    where TContext : DbContext
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<DeadLetterSummary>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        await context.Set<DeadLetterMessage>()
            .AsNoTracking()
            .Where(predicate: row => row.ReplayedOnUtc == null)
            .OrderByDescending(keySelector: row => row.DeadLetteredOnUtc)
            .Skip(count: skip)
            .Take(count: take)
            .Select(selector: row => new DeadLetterSummary(
                Id: row.Id,
                OutboxMessageId: row.OutboxMessageId,
                Handler: row.Handler,
                Type: row.Type,
                OccurredOnUtc: row.OccurredOnUtc,
                DeadLetteredOnUtc: row.DeadLetteredOnUtc,
                Attempts: row.Attempts,
                Error: row.Error,
                ReplayCount: row.ReplayCount))
            .ToListAsync(cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async ValueTask<DeadLetterReplayResult> ReplayAsync(Guid deadLetterId, CancellationToken cancellationToken = default)
    {
        DeadLetterMessage? row = await context.Set<DeadLetterMessage>()
            .FirstOrDefaultAsync(predicate: m => m.Id == deadLetterId && m.ReplayedOnUtc == null, cancellationToken: cancellationToken);

        if (row is null)
        {
            return DeadLetterReplayResult.NotFound;
        }

        IDomainEvent? domainEvent = Deserialize(row: row);

        if (domainEvent is null || row.Handler is null)
        {
            logger.LogError(
                message: "Dead letter {DeadLetterId} of type {Type} cannot be replayed: unresolvable event type, unreadable payload, or no handler recorded.",
                row.Id, row.Type);

            return DeadLetterReplayResult.NotReplayable;
        }

        DateTime attemptedOnUtc = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await publisher.PublishToHandlerAsync(
                domainEvent: domainEvent,
                handlerName: row.Handler,
                cancellationToken: cancellationToken);

            row.MarkReplayed(replayedOnUtc: attemptedOnUtc);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);

            return DeadLetterReplayResult.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Replaying something still broken is the expected case, not an exceptional one — record
            // the newer error and leave the row for the next attempt or for a person.
            logger.LogError(exception: ex, message: "Replay of dead letter {DeadLetterId} failed again.", args: row.Id);

            row.MarkReplayFailed(attemptedOnUtc: attemptedOnUtc, error: Truncate(value: ex.ToString(), maxLength: 4000));
            await context.SaveChangesAsync(cancellationToken: cancellationToken);

            return DeadLetterReplayResult.Failed;
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> DiscardAsync(Guid deadLetterId, CancellationToken cancellationToken = default)
    {
        DeadLetterMessage? row = await context.Set<DeadLetterMessage>()
            .FirstOrDefaultAsync(predicate: m => m.Id == deadLetterId, cancellationToken: cancellationToken);

        if (row is null)
        {
            return false;
        }

        context.Set<DeadLetterMessage>().Remove(entity: row);
        await context.SaveChangesAsync(cancellationToken: cancellationToken);

        return true;
    }

    /// <summary>
    /// Reads the stored payload back into its concrete event type, using the same serializer options
    /// the outbox wrote it with.
    /// </summary>
    /// <param name="row">The dead letter to read.</param>
    /// <returns>The event, or <see langword="null"/> when its type or payload cannot be recovered.</returns>
    private IDomainEvent? Deserialize(DeadLetterMessage row)
    {
        if (!registry.TryResolve(name: row.Type, domainEventType: out Type? domainEventType))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json: row.Content, returnType: domainEventType, options: OutboxSerialization.Options) as IDomainEvent;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
