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
using Microsoft.Extensions.Options;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Infrastructure.Persistence.DeadLetters;
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

        // The fourth parameter leases the claimed rows: they stay invisible to any other processor
        // until this batch could plausibly have failed. See OutboxClaimSql for why SKIP LOCKED alone
        // does not cover the window between claiming a row and finishing with it.
        List<OutboxMessage> claimed = await context.Set<OutboxMessage>()
            .FromSqlRaw(sql: OutboxClaimSql.Text, outboxOptions.MaxAttempts, claimedAt, outboxOptions.BatchSize, claimedAt + outboxOptions.BaseBackoff)
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
                    Fail(message: message, outboxOptions: outboxOptions, error: $"Deserializing outbox message '{message.Id}' of type '{message.Type}' produced null.", failures: []);
                }
                else
                {
                    // Every handler is attempted regardless of what its siblings did; the report says
                    // which ones threw. A failure is therefore a per-handler fact from here on.
                    DomainEventDeliveryReport report = await publisher.PublishAsync(
                        domainEvents: [(IDomainEvent)domainEvent],
                        cancellationToken: cancellationToken);

                    if (report.HasFailures)
                    {
                        Fail(message: message, outboxOptions: outboxOptions, error: report.Describe(), failures: report.Failures);
                    }
                    else
                    {
                        message.MarkProcessed(processedOnUtc: timeProvider.GetUtcNow().UtcDateTime);
                    }
                }
            }
            else
            {
                Fail(message: message, outboxOptions: outboxOptions, error: $"No domain event type is registered for outbox contract name '{message.Type}'.", failures: []);
            }

            await context.SaveChangesAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Fail(message: message, outboxOptions: outboxOptions, error: Truncate(value: ex.ToString(), maxLength: 4000), failures: []);

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

    /// <summary>
    /// Records a failed attempt, and moves the message to the dead-letter queue once it has run out
    /// of attempts.
    /// </summary>
    /// <param name="message">The message that failed.</param>
    /// <param name="outboxOptions">The backoff and attempt-limit settings.</param>
    /// <param name="error">The aggregated error text to record.</param>
    /// <param name="failures">
    /// The per-handler failures, when delivery reached handlers at all. Empty for a failure that
    /// happened before any handler ran — an unresolvable contract name, or a payload that would not
    /// deserialize — which dead-letters as a single row with no handler.
    /// </param>
    private void Fail(OutboxMessage message, OutboxOptions outboxOptions, string error, IReadOnlyList<DomainEventHandlerFailure> failures)
    {
        // `message.Attempts` already reflects this attempt — the claim query increments it before
        // this method ever runs — so the exponent below counts attempts already made, not a
        // predicted next one.
        TimeSpan backoff = outboxOptions.BaseBackoff * Math.Pow(x: 2, y: message.Attempts - 1);
        message.MarkFailed(error: Truncate(value: error, maxLength: 4000), nextAttemptOnUtc: timeProvider.GetUtcNow().UtcDateTime + backoff);

        if (message.Attempts < outboxOptions.MaxAttempts)
        {
            return;
        }

        logger.LogCritical(
            message: "Outbox message {MessageId} of type {MessageType} reached {Attempts} attempts and is being dead-lettered.",
            message.Id, message.Type, message.Attempts);

        DeadLetter(message: message, error: error, failures: failures);
    }

    /// <summary>
    /// Writes one dead-letter row per failed handler and removes the outbox message.
    /// </summary>
    /// <remarks>
    /// Both happen through the change tracker, so the caller's single <c>SaveChangesAsync</c> commits
    /// them in one transaction — the move can never half-happen, leaving the message both queued and
    /// dead-lettered, or neither.
    /// </remarks>
    /// <param name="message">The exhausted message.</param>
    /// <param name="error">The aggregated error text, used when there is no per-handler detail.</param>
    /// <param name="failures">The per-handler failures; empty when delivery never reached a handler.</param>
    private void DeadLetter(OutboxMessage message, string error, IReadOnlyList<DomainEventHandlerFailure> failures)
    {
        DateTime deadLetteredOnUtc = timeProvider.GetUtcNow().UtcDateTime;

        IEnumerable<DeadLetterMessage> rows = failures.Count == 0
            ? [Row(handler: null, rowError: error)]
            : failures.Select(selector: failure => Row(handler: failure.HandlerName, rowError: failure.Exception.ToString()));

        context.Set<DeadLetterMessage>().AddRange(entities: rows);
        context.Set<OutboxMessage>().Remove(entity: message);

        DeadLetterMessage Row(string? handler, string rowError) =>
            new(outboxMessageId: message.Id,
                handler: handler,
                type: message.Type,
                content: message.Content,
                occurredOnUtc: message.OccurredOnUtc,
                deadLetteredOnUtc: deadLetteredOnUtc,
                attempts: message.Attempts,
                error: Truncate(value: rowError, maxLength: 4000));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
