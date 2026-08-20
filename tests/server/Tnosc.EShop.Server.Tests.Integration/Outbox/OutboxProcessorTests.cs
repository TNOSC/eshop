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
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Outbox;

/// <summary>
/// Behaviour tests for <see cref="IOutboxProcessor"/> — claiming, publishing, marking processed,
/// exponential backoff on failure, dead-lettering past <c>MaxAttempts</c>, poison-message isolation,
/// and the <c>FOR UPDATE SKIP LOCKED</c> concurrency guarantee.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class OutboxProcessorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    [Fact]
    public async Task ProcessBatchAsync_Should_ClaimPublishAndMarkProcessed_When_MessagesArePending()
    {
        OutboxMessage first = CreateGoodMessage(aggregateId: out Guid firstAggregateId);
        OutboxMessage second = CreateGoodMessage(aggregateId: out Guid secondAggregateId);
        await SeedAsync(first, second);

        int claimed = await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        claimed.ShouldBe(expected: 2);

        List<OutboxMessage> rows = await WriteContext.Set<OutboxMessage>().AsNoTracking().ToListAsync();
        rows.ShouldAllBe(elementPredicate: row => row.ProcessedOnUtc != null && row.Error == null);

        Spy.Delivered().ShouldBe(expected: [firstAggregateId, secondAggregateId], ignoreOrder: true);
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_IncrementAttemptsAndScheduleBackoff_When_TheHandlerThrows()
    {
        OutboxMessage poison = CreatePoisonMessage();
        await SeedAsync(entities: poison);

        DateTime now = TimeProvider.GetUtcNow().UtcDateTime;

        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking().SingleAsync(predicate: m => m.Id == poison.Id);

        row.ProcessedOnUtc.ShouldBeNull();
        // The claim query increments Attempts by 1 as part of claiming, and OutboxMessage.MarkFailed
        // increments it again when Fail() records the failure — one failed attempt nets +2.
        row.Attempts.ShouldBe(expected: 2);
        row.Error.ShouldNotBeNullOrWhiteSpace();

        // BaseBackoff defaults to 10s; exponent is (Attempts - 1) = 0, so the first failure backs off
        // by exactly one BaseBackoff unit. TestTimeProvider is frozen, so this is deterministic modulo
        // Postgres's microsecond timestamp precision vs. .NET's 100ns ticks.
        row.NextAttemptOnUtc.ShouldNotBeNull();
        row.NextAttemptOnUtc.Value.ShouldBe(expected: now + TimeSpan.FromSeconds(seconds: 10), tolerance: TimeSpan.FromMilliseconds(milliseconds: 1));
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_NotClaim_When_AttemptsAlreadyReachedMaxAttempts()
    {
        const int maxAttempts = 5;

        OutboxMessage exhausted = CreatePoisonMessage();
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            exhausted.MarkFailed(error: "prior failure", nextAttemptOnUtc: TimeProvider.GetUtcNow().UtcDateTime.AddSeconds(value: -1));
        }

        await SeedAsync(entities: exhausted);

        int claimed = await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        claimed.ShouldBe(expected: 0);

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking().SingleAsync(predicate: m => m.Id == exhausted.Id);
        row.Attempts.ShouldBe(expected: maxAttempts);
        row.ProcessedOnUtc.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_ProcessTheGoodMessages_When_TheBatchAlsoContainsAPoisonMessage()
    {
        OutboxMessage poison = CreatePoisonMessage();
        OutboxMessage first = CreateGoodMessage(aggregateId: out Guid firstAggregateId);
        OutboxMessage second = CreateGoodMessage(aggregateId: out Guid secondAggregateId);
        await SeedAsync(poison, first, second);

        int claimed = await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        claimed.ShouldBe(expected: 3);

        OutboxMessage poisonRow = await WriteContext.Set<OutboxMessage>().AsNoTracking().SingleAsync(predicate: m => m.Id == poison.Id);
        poisonRow.ProcessedOnUtc.ShouldBeNull();
        // The claim query increments Attempts by 1 as part of claiming, and OutboxMessage.MarkFailed
        // increments it again when Fail() records the failure — one failed attempt nets +2.
        poisonRow.Attempts.ShouldBe(expected: 2);

        List<OutboxMessage> goodRows = await WriteContext.Set<OutboxMessage>().AsNoTracking()
            .Where(predicate: m => m.Id == first.Id || m.Id == second.Id)
            .ToListAsync();
        goodRows.ShouldAllBe(elementPredicate: row => row.ProcessedOnUtc != null);

        Spy.Delivered().ShouldBe(expected: [firstAggregateId, secondAggregateId], ignoreOrder: true);
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_NeverDeliverTheSameMessageTwice_When_TwoProcessorsRunConcurrently()
    {
        const int messageCount = 40; // Comfortably above the default BatchSize (20) on each side.

        var aggregateIds = new List<Guid>(capacity: messageCount);
        var messages = new List<OutboxMessage>(capacity: messageCount);
        for (int i = 0; i < messageCount; i++)
        {
            messages.Add(item: CreateGoodMessage(aggregateId: out Guid aggregateId));
            aggregateIds.Add(item: aggregateId);
        }

        await SeedAsync(entities: messages.Cast<object>().ToArray());

        Task<int> runA = RunProcessorInNewScopeAsync();
        Task<int> runB = RunProcessorInNewScopeAsync();
        int[] claimedCounts = await Task.WhenAll(runA, runB);

        claimedCounts.Sum().ShouldBe(expected: messageCount);

        IReadOnlyList<Guid> delivered = Spy.Delivered();
        delivered.Count.ShouldBe(expected: messageCount);
        delivered.Distinct().Count().ShouldBe(expected: messageCount);
    }

    private async Task<int> RunProcessorInNewScopeAsync()
    {
        using IServiceScope scope = Fixture.Services.CreateScope();
        IOutboxProcessor processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        return await processor.ProcessBatchAsync(cancellationToken: CancellationToken.None);
    }

    private static OutboxMessage CreateGoodMessage(out Guid aggregateId)
    {
        aggregateId = Guid.NewGuid();
        var domainEvent = new TestAggregateCreatedDomainEvent(Id: Guid.NewGuid(), OccurredOnUtc: DateTime.UtcNow, AggregateId: aggregateId, Name: "widget", Note: "note", Amount: 1, Tags: ["tag"]);
        return new OutboxMessage(type: "test.aggregate-created.v1", content: JsonSerializer.Serialize(value: domainEvent, options: SerializerOptions));
    }

    private static OutboxMessage CreatePoisonMessage()
    {
        var domainEvent = new PoisonTestDomainEvent(Id: Guid.NewGuid(), OccurredOnUtc: DateTime.UtcNow, AggregateId: Guid.NewGuid());
        return new OutboxMessage(type: "test.poison-event.v1", content: JsonSerializer.Serialize(value: domainEvent, options: SerializerOptions));
    }
}
