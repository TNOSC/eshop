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
            .FromSqlRaw(sql: OutboxClaimSql.Text, outboxOptions.MaxAttempts, claimedAt, outboxOptions.BatchSize)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (OutboxMessage message in claimed)
        {
            await ProcessOneAsync(message: message, outboxOptions: outboxOptions, cancellationToken: cancellationToken);
        }

        return claimed.Count;
    }

    private async ValueTask ProcessOneAsync(OutboxMessage message, OutboxOptions outboxOptions, CancellationToken cancellationToken)
    {
        try
        {
            if (registry.TryResolve(name: message.Type, domainEventType: out Type? domainEventType))
            {
                object? domainEvent = JsonSerializer.Deserialize(json: message.Content, returnType: domainEventType, options: OutboxSerialization.Options);

                if (domainEvent is null)
                {
                    Fail(message: message, outboxOptions: outboxOptions, error: $"Deserializing outbox message '{message.Id}' of type '{message.Type}' produced null.");
                }
                else
                {
                    await publisher.PublishAsync(domainEvents: [(IDomainEvent)domainEvent], cancellationToken: cancellationToken);
                    message.MarkProcessed(processedOnUtc: timeProvider.GetUtcNow().UtcDateTime);
                }
            }
            else
            {
                Fail(message: message, outboxOptions: outboxOptions, error: $"No domain event type is registered for outbox contract name '{message.Type}'.");
            }

            await context.SaveChangesAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Fail(message: message, outboxOptions: outboxOptions, error: Truncate(value: ex.ToString(), maxLength: 4000));

            try
            {
                await context.SaveChangesAsync(cancellationToken: cancellationToken);
            }
            catch (Exception saveEx)
            {
                logger.LogError(exception: saveEx, message: "Failed to persist failure state for outbox message {MessageId}.", args: message.Id);
            }
        }
    }

    private void Fail(OutboxMessage message, OutboxOptions outboxOptions, string error)
    {
        // `message.Attempts` already reflects this attempt — the claim query increments it before
        // this method ever runs — so the exponent below counts attempts already made, not a
        // predicted next one.
        TimeSpan backoff = outboxOptions.BaseBackoff * Math.Pow(x: 2, y: message.Attempts - 1);
        message.MarkFailed(error: Truncate(value: error, maxLength: 4000), nextAttemptOnUtc: timeProvider.GetUtcNow().UtcDateTime + backoff);

        if (message.Attempts >= outboxOptions.MaxAttempts)
        {
            logger.LogCritical(
                message: "Outbox message {MessageId} of type {MessageType} reached {Attempts} attempts and will no longer be claimed.",
                message.Id, message.Type, message.Attempts);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
