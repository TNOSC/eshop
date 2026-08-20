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
using Shouldly;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Infrastructure.Persistence.DeadLetters;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Outbox;

/// <summary>
/// Fan-out isolation and the dead-letter queue: one broken handler neither blocks its siblings nor
/// drags them into the DLQ, and the handler that does fail permanently ends up as a row that names
/// it and can be replayed on its own.
/// </summary>
/// <remarks>
/// The event used here has two registered handlers, the broken one registered first. Everything runs
/// through the real <c>OutboxProcessor</c> and the real decorator chain, which is what makes the
/// isolation claim mean something.
/// </remarks>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class DeadLetterTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int MaxAttempts = 5;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    [Fact]
    public async Task ProcessBatchAsync_Should_ClaimTheInboxPerHandler_When_OneEventHasSeveralHandlers()
    {
        // Arrange
        await SeedAsync(CreateMessage(aggregateId: out _));

        // Act
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        // Regression guard. Both handlers of an event share one closed decorator type, so memoising
        // a handler's identity against that type gave the sibling the first one's name — they then
        // shared a single inbox claim and only one of them ever ran.
        WriteContext.ChangeTracker.Clear();
        List<ProcessedEvent> claims = await WriteContext.Set<ProcessedEvent>().AsNoTracking().ToListAsync();

        claims.ShouldHaveSingleItem().Handler.ShouldBe(
            expected: typeof(FanOutSucceedingHandler).FullName,
            customMessage: "only the handler that succeeded may hold a claim — the failing one's rolled back with its work");
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_StillRunTheSiblingHandler_When_AnEarlierHandlerThrows()
    {
        // Arrange
        await SeedAsync(CreateMessage(aggregateId: out Guid aggregateId));

        // Act
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        FanOutPlan.FailingHandlerInvocations.ShouldBe(expected: 1);
        Spy.Delivered().ShouldBe(
            expected: [aggregateId],
            customMessage: "the handler registered after the failing one must still receive the event");
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_DeadLetterOnlyTheFailingHandler_When_AttemptsAreExhausted()
    {
        // Arrange
        await SeedAsync(CreateMessage(aggregateId: out Guid aggregateId));

        // Act
        await DrainUntilDeadLetteredAsync();

        // Assert
        DeadLetterMessage row = await SingleDeadLetterAsync();
        row.Handler.ShouldBe(
            expected: typeof(FanOutFailingHandler).FullName,
            customMessage: "the row must name the handler that failed, not the message");
        row.Type.ShouldBe(expected: "test.fan-out-event.v1");
        row.Error.ShouldContain(expected: "refuses event");
        row.Attempts.ShouldBeGreaterThanOrEqualTo(expected: MaxAttempts);

        (await WriteContext.Set<OutboxMessage>().CountAsync()).ShouldBe(
            expected: 0,
            customMessage: "an exhausted message moves to the DLQ rather than lingering in the outbox forever");

        Spy.Delivered().ShouldBe(
            expected: [aggregateId],
            customMessage: "the healthy handler ran once on the first pass and its inbox claim kept it from re-running");
        FanOutPlan.SucceedingHandlerInvocations.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_DeadLetterWithNoHandler_When_TheContractNameIsUnknown()
    {
        // Arrange
        await SeedAsync(new OutboxMessage(type: "test.no-such-event.v1", content: "{}"));

        // Act
        await DrainUntilDeadLetteredAsync();

        // Assert
        DeadLetterMessage row = await SingleDeadLetterAsync();
        row.Handler.ShouldBeNull(customMessage: "delivery never reached a handler, so there is none to name");
        row.Error.ShouldContain(expected: "No domain event type is registered");

        (await WriteContext.Set<OutboxMessage>().CountAsync()).ShouldBe(
            expected: 0,
            customMessage: "an undeliverable message must not become a zombie outbox row either");
    }

    [Fact]
    public async Task ReplayAsync_Should_RunOnlyTheFailedHandler_When_TheCauseIsFixed()
    {
        // Arrange
        await SeedAsync(CreateMessage(aggregateId: out _));
        await DrainUntilDeadLetteredAsync();

        DeadLetterMessage row = await SingleDeadLetterAsync();
        int deliveriesBeforeReplay = Spy.Delivered().Count;
        int succeedingInvocationsBeforeReplay = FanOutPlan.SucceedingHandlerInvocations;

        FanOutPlan.FailingHandlerShouldFail = false;

        // Act
        DeadLetterReplayResult result = await DeadLetterQueue.ReplayAsync(deadLetterId: row.Id, cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBe(expected: DeadLetterReplayResult.Succeeded);

        FanOutPlan.SucceedingHandlerInvocations.ShouldBe(
            expected: succeedingInvocationsBeforeReplay,
            customMessage: "replay is handler-scoped — the sibling that already succeeded must not run again");
        Spy.Delivered().Count.ShouldBe(expected: deliveriesBeforeReplay);

        WriteContext.ChangeTracker.Clear();
        DeadLetterMessage replayed = await WriteContext.Set<DeadLetterMessage>().AsNoTracking().SingleAsync();
        replayed.ReplayedOnUtc.ShouldNotBeNull();
        replayed.ReplayCount.ShouldBe(expected: 1);

        (await DeadLetterQueue.ListAsync(skip: 0, take: 10, cancellationToken: CancellationToken.None))
            .ShouldBeEmpty(customMessage: "a recovered row stays as an audit trail but leaves the pending queue");
    }

    [Fact]
    public async Task ReplayAsync_Should_RecordTheAttemptAndKeepTheRow_When_TheHandlerStillFails()
    {
        // Arrange
        await SeedAsync(CreateMessage(aggregateId: out _));
        await DrainUntilDeadLetteredAsync();

        DeadLetterMessage row = await SingleDeadLetterAsync();

        // Act
        // The cause has not been fixed, so this replay fails exactly as the original delivery did.
        DeadLetterReplayResult result = await DeadLetterQueue.ReplayAsync(deadLetterId: row.Id, cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBe(expected: DeadLetterReplayResult.Failed, customMessage: "a still-broken replay is an outcome, not an exception");

        WriteContext.ChangeTracker.Clear();
        DeadLetterMessage stillPending = await WriteContext.Set<DeadLetterMessage>().AsNoTracking().SingleAsync();
        stillPending.ReplayCount.ShouldBe(expected: 1);
        stillPending.LastReplayedOnUtc.ShouldNotBeNull();
        stillPending.ReplayedOnUtc.ShouldBeNull();

        (await DeadLetterQueue.ListAsync(skip: 0, take: 10, cancellationToken: CancellationToken.None))
            .Count.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task ListAsync_Should_ProjectTheRowForTriage()
    {
        // Arrange
        await SeedAsync(CreateMessage(aggregateId: out _));
        await DrainUntilDeadLetteredAsync();

        // Act
        IReadOnlyList<DeadLetterSummary> pending = await DeadLetterQueue.ListAsync(skip: 0, take: 10, cancellationToken: CancellationToken.None);

        // Assert
        DeadLetterSummary summary = pending.ShouldHaveSingleItem();
        summary.Handler.ShouldBe(expected: typeof(FanOutFailingHandler).FullName);
        summary.Type.ShouldBe(expected: "test.fan-out-event.v1");
        summary.Attempts.ShouldBeGreaterThanOrEqualTo(expected: MaxAttempts);
        summary.ReplayCount.ShouldBe(expected: 0);
    }

    [Fact]
    public async Task DiscardAsync_Should_RemoveTheRow()
    {
        // Arrange
        await SeedAsync(CreateMessage(aggregateId: out _));
        await DrainUntilDeadLetteredAsync();

        DeadLetterMessage row = await SingleDeadLetterAsync();

        // Act
        bool discarded = await DeadLetterQueue.DiscardAsync(deadLetterId: row.Id, cancellationToken: CancellationToken.None);

        // Assert
        discarded.ShouldBeTrue();

        WriteContext.ChangeTracker.Clear();
        (await WriteContext.Set<DeadLetterMessage>().CountAsync()).ShouldBe(expected: 0);
    }

    [Fact]
    public async Task ReplayAsync_Should_ReportNotFound_When_TheRowDoesNotExist()
    {
        // Act
        DeadLetterReplayResult result = await DeadLetterQueue.ReplayAsync(
            deadLetterId: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.ShouldBe(expected: DeadLetterReplayResult.NotFound);
    }

    /// <summary>
    /// Runs the processor until the message runs out of attempts, advancing the clock past each
    /// backoff so the next pass can claim the row.
    /// </summary>
    private async Task DrainUntilDeadLetteredAsync()
    {
        // Attempts nets +2 per failed pass — the claim query increments it and MarkFailed increments
        // it again — so MaxAttempts of 5 is reached on the third pass. One spare iteration guards the
        // loop against that arithmetic changing without this test noticing.
        for (int pass = 0; pass < 5; pass++)
        {
            if (await WriteContext.Set<DeadLetterMessage>().AsNoTracking().AnyAsync())
            {
                return;
            }

            await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);
            WriteContext.ChangeTracker.Clear();
            TimeProvider.Advance(delta: TimeSpan.FromMinutes(value: 30));
        }
    }

    private async Task<DeadLetterMessage> SingleDeadLetterAsync()
    {
        WriteContext.ChangeTracker.Clear();

        return await WriteContext.Set<DeadLetterMessage>().AsNoTracking().SingleAsync();
    }

    private static OutboxMessage CreateMessage(out Guid aggregateId)
    {
        aggregateId = Guid.NewGuid();

        var domainEvent = new FanOutTestDomainEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            AggregateId: aggregateId);

        return new OutboxMessage(
            type: "test.fan-out-event.v1",
            content: JsonSerializer.Serialize(value: domainEvent, options: SerializerOptions));
    }
}
