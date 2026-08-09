// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Infrastructure.Persistence.Publishers;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Claims and processes a batch of pending outbox messages against <typeparamref name="TContext"/>.
/// </summary>
/// <typeparam name="TContext">The write <see cref="DbContext"/> that owns the outbox table.</typeparam>
internal sealed class OutboxProcessor<TContext>(
    TContext context,
    IDomainEventTypeRegistry registry,
    IDomainEventsPublisher publisher,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor<TContext>> logger) : IOutboxProcessor
    where TContext : DbContext
{
    /// <inheritdoc />
    public async ValueTask<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        OutboxOptions outboxOptions = options.Value;
        DateTime claimedAt = timeProvider.GetUtcNow().UtcDateTime;

        List<OutboxMessage> claimed = await context.Set<OutboxMessage>()
            .FromSqlRaw(OutboxClaimSql.Text, outboxOptions.MaxAttempts, claimedAt, outboxOptions.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (OutboxMessage message in claimed)
        {
            await ProcessOneAsync(message, outboxOptions, cancellationToken);
        }

        return claimed.Count;
    }

    private async ValueTask ProcessOneAsync(OutboxMessage message, OutboxOptions outboxOptions, CancellationToken cancellationToken)
    {
        try
        {
            if (registry.TryResolve(message.Type, out Type? domainEventType))
            {
                object? domainEvent = JsonSerializer.Deserialize(message.Content, domainEventType, OutboxSerialization.Options);

                if (domainEvent is null)
                {
                    Fail(message, outboxOptions, $"Deserializing outbox message '{message.Id}' of type '{message.Type}' produced null.");
                }
                else
                {
                    await publisher.PublishAsync([(IDomainEvent)domainEvent], cancellationToken);
                    message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                }
            }
            else
            {
                Fail(message, outboxOptions, $"No domain event type is registered for outbox contract name '{message.Type}'.");
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Fail(message, outboxOptions, Truncate(ex.ToString(), 4000));

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx, "Failed to persist failure state for outbox message {MessageId}.", message.Id);
            }
        }
    }

    private void Fail(OutboxMessage message, OutboxOptions outboxOptions, string error)
    {
        // `message.Attempts` already reflects this attempt — the claim query increments it before
        // this method ever runs — so the exponent below counts attempts already made, not a
        // predicted next one.
        TimeSpan backoff = outboxOptions.BaseBackoff * Math.Pow(2, message.Attempts - 1);
        message.MarkFailed(Truncate(error, 4000), timeProvider.GetUtcNow().UtcDateTime + backoff);

        if (message.Attempts >= outboxOptions.MaxAttempts)
        {
            logger.LogCritical(
                "Outbox message {MessageId} of type {MessageType} reached {Attempts} attempts and will no longer be claimed.",
                message.Id, message.Type, message.Attempts);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
