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
using Shouldly;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Outbox;

/// <summary>
/// <c>[Retry]</c> on a domain event handler, driven through the real decorator chain: a transient
/// failure is absorbed in-process so the outbox never sees it, and one that outlasts the attempts
/// still falls through to the outbox's durable retry.
/// </summary>
/// <remarks>
/// Resolving the handler through <c>OutboxProcessor</c> rather than constructing the decorator is
/// the whole point — it is what proves the <c>TryDecorate</c> registration and, in particular, that
/// <c>Retry</c> wraps <c>Idempotency</c> rather than the other way round. Constructed directly, the
/// ordering bug this guards against would be invisible.
/// </remarks>
[Collection(nameof(PostgresCollection))]
public sealed class DomainEventRetryTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    [Fact]
    public async Task ProcessBatchAsync_Should_AbsorbTheFailureInProcess_When_ALaterAttemptSucceeds()
    {
        // Arrange
        FlakyPlan.FailuresBeforeSuccess = 1;
        OutboxMessage message = CreateMessage(aggregateId: out Guid aggregateId);
        await SeedAsync(message);

        // Act
        int claimed = await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        claimed.ShouldBe(expected: 1);
        FlakyPlan.Invocations.ShouldBe(expected: 2, customMessage: "the first attempt failed and the retry decorator ran a second");
        Spy.Delivered().ShouldBe(expected: [aggregateId], customMessage: "the retried attempt must deliver exactly once");

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
        row.ProcessedOnUtc.ShouldNotBeNull();
        row.Error.ShouldBeNull(customMessage: "a failure absorbed in-process must never reach the outbox as an error");
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_LeaveNoInboxClaim_When_TheFailedAttemptRollsBack()
    {
        // Arrange
        FlakyPlan.FailuresBeforeSuccess = 1;
        await SeedAsync(CreateMessage(aggregateId: out _));

        // Act
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        WriteContext.ChangeTracker.Clear();
        (await WriteContext.Set<ProcessedEvent>().CountAsync()).ShouldBe(
            expected: 1,
            customMessage: "the failed attempt's claim must roll back, leaving only the successful attempt's");
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_FallThroughToTheOutbox_When_EveryAttemptFails()
    {
        // Arrange
        // More failures than [Retry(3)] allows, so the in-process retry is exhausted.
        FlakyPlan.FailuresBeforeSuccess = int.MaxValue;
        await SeedAsync(CreateMessage(aggregateId: out _));

        // Act
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        FlakyPlan.Invocations.ShouldBe(expected: 3, customMessage: "[Retry(3)] is three attempts in total");
        Spy.Delivered().ShouldBeEmpty();

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
        row.ProcessedOnUtc.ShouldBeNull();
        row.Error.ShouldNotBeNull(customMessage: "an exhausted in-process retry must still hand the failure to the durable one");
        row.NextAttemptOnUtc.ShouldNotBeNull(customMessage: "and the outbox must schedule its own backoff");
    }

    private static OutboxMessage CreateMessage(out Guid aggregateId)
    {
        aggregateId = Guid.NewGuid();

        var domainEvent = new FlakyTestDomainEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            AggregateId: aggregateId);

        return new OutboxMessage(
            type: "test.flaky-event.v1",
            content: JsonSerializer.Serialize(value: domainEvent, options: SerializerOptions));
    }
}
