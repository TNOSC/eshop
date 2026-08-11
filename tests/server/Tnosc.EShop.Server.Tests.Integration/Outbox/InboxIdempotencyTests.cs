// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Outbox;

/// <summary>
/// The inbox closing the outbox's at-least-once redelivery window: a handler marked
/// <c>[Idempotent]</c> claims the event id in the same transaction as its own work, so a
/// redelivered event is recognised and skipped instead of applying its effect twice.
/// </summary>
/// <remarks>
/// Redelivery is reproduced by resetting a processed row back to pending, which is exactly the state
/// a crash between publishing and marking the message processed leaves behind — the one window the
/// processor cannot close on its own.
/// </remarks>
[Collection(nameof(PostgresCollection))]
public sealed class InboxIdempotencyTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    [Fact]
    public async Task ProcessBatchAsync_Should_DeliverOnce_When_TheSameEventIsRedelivered()
    {
        // Arrange
        OutboxMessage message = CreateMessage(aggregateId: out Guid aggregateId);
        await SeedAsync(message);

        // Act
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        Spy.Delivered().ShouldBe(expected: [aggregateId]);
        (await ClaimCountAsync()).ShouldBe(expected: 1, customMessage: "the first delivery must record an inbox claim");

        // Arrange
        await ResetToPendingAsync();

        // Act
        int reclaimed = await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        reclaimed.ShouldBe(expected: 1, customMessage: "the processor must still pick the row up — dedupe is the handler's job, not the processor's");
        Spy.Delivered().ShouldBe(expected: [aggregateId], customMessage: "a redelivered event must not reach the handler a second time");
        (await ClaimCountAsync()).ShouldBe(expected: 1, customMessage: "the redelivery must not add a second claim");

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
        row.ProcessedOnUtc.ShouldNotBeNull(customMessage: "a skipped redelivery still counts as processed, or the row would loop forever");
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_ScopeTheClaimToTheHandler_When_TheEventIsProcessed()
    {
        // Arrange
        OutboxMessage message = CreateMessage(aggregateId: out Guid aggregateId);
        await SeedAsync(message);

        // Act
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        ProcessedEvent claim = await WriteContext.Set<ProcessedEvent>().AsNoTracking().SingleAsync();
        claim.Handler.ShouldBe(
            expected: typeof(TestDomainEventHandler).FullName,
            customMessage: "the claim must name the unwrapped handler, so each handler of an event dedupes independently");
        claim.EventId.ShouldNotBe(expected: Guid.Empty);
        claim.EventId.ShouldNotBe(expected: aggregateId, customMessage: "the claim is keyed on the event id, not on the aggregate it concerns");
    }

    private async Task<int> ClaimCountAsync()
    {
        WriteContext.ChangeTracker.Clear();

        return await WriteContext.Set<ProcessedEvent>().CountAsync();
    }

    /// <summary>
    /// Puts the processed row back into the state a crash between publish and mark would leave it in.
    /// </summary>
    private async Task ResetToPendingAsync() =>
        await WriteContext.Database.ExecuteSqlRawAsync(
            sql: $"UPDATE {OutboxMessageConfiguration.SchemaName}.{OutboxMessageConfiguration.TableName} SET processed_on_utc = NULL, attempts = 0, next_attempt_on_utc = NULL");

    private static OutboxMessage CreateMessage(out Guid aggregateId)
    {
        aggregateId = Guid.NewGuid();

        var domainEvent = new TestAggregateCreatedDomainEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            AggregateId: aggregateId,
            Name: "widget",
            Note: "note",
            Amount: 1,
            Tags: ["tag"]);

        return new OutboxMessage(
            type: "test.aggregate-created.v1",
            content: JsonSerializer.Serialize(value: domainEvent, options: SerializerOptions));
    }
}
