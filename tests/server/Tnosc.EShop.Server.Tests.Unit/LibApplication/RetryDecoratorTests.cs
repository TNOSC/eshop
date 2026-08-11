// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Exceptions;
using Tnosc.Lib.Domain;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibApplication;

/// <summary>
/// The domain event half of <see cref="RetryDecorator"/>: how many times the handler actually runs,
/// for each combination of <c>[Retry]</c> and the kind of failure thrown.
/// </summary>
/// <remarks>
/// Every scenario uses its own event type on purpose. The decorator memoises the <c>[Retry]</c>
/// lookup against its own closed generic type, which in production identifies the inner handler
/// uniquely because DI registers exactly one handler per closed interface. Reusing one event type
/// across handlers with different attributes would collide on that cache in a way production cannot.
/// </remarks>
public sealed class RetryDecoratorTests
{
    [Fact]
    public async Task HandleAsync_Should_RunOnceAndPropagate_When_TheHandlerDeclaresNoRetry()
    {
        // Arrange
        var inner = new UnmarkedHandler { Throws = Retriable };
        var decorator = new RetryDecorator.DomainEventHandler<UnmarkedEvent>(innerHandler: inner);

        // Act
        await Should.ThrowAsync<TransientFailureException>(
            actual: async () => await decorator.HandleAsync(@event: new UnmarkedEvent(), cancellationToken: CancellationToken.None));

        // Assert
        inner.Calls.ShouldBe(
            expected: 1,
            customMessage: "retry on the event pipeline is strictly opt-in — without [Retry] the outbox is the only retry");
    }

    [Fact]
    public async Task HandleAsync_Should_ExhaustAttemptsThenPropagate_When_TheFailureKeepsRecurring()
    {
        // Arrange
        var inner = new AlwaysFailingHandler { Throws = Retriable };
        var decorator = new RetryDecorator.DomainEventHandler<AlwaysFailingEvent>(innerHandler: inner);

        // Act
        await Should.ThrowAsync<TransientFailureException>(
            actual: async () => await decorator.HandleAsync(@event: new AlwaysFailingEvent(), cancellationToken: CancellationToken.None));

        // Assert
        inner.Calls.ShouldBe(
            expected: 3,
            customMessage: "[Retry(3)] is three attempts in total, and the last failure must still reach the outbox");
    }

    [Fact]
    public async Task HandleAsync_Should_StopRetrying_When_AnAttemptSucceeds()
    {
        // Arrange
        var inner = new RecoveringHandler { FailuresBeforeSuccess = 1, Throws = Retriable };
        var decorator = new RetryDecorator.DomainEventHandler<RecoveringEvent>(innerHandler: inner);

        // Act
        await decorator.HandleAsync(@event: new RecoveringEvent(), cancellationToken: CancellationToken.None);

        // Assert
        inner.Calls.ShouldBe(expected: 2, customMessage: "a transient blip must be absorbed without reaching the outbox");
    }

    [Fact]
    public async Task HandleAsync_Should_NotRetry_When_TheExceptionIsNotRetriable()
    {
        // Arrange
        var inner = new NonRetriableHandler { Throws = static () => new ConflictException(message: "taken", correlationId: null, inner: null) };
        var decorator = new RetryDecorator.DomainEventHandler<NonRetriableEvent>(innerHandler: inner);

        // Act
        await Should.ThrowAsync<ConflictException>(
            actual: async () => await decorator.HandleAsync(@event: new NonRetriableEvent(), cancellationToken: CancellationToken.None));

        // Assert
        inner.Calls.ShouldBe(expected: 1, customMessage: "a conflict is a decision, not a blip — retrying it would only delay the same answer");
    }

    [Fact]
    public async Task HandleAsync_Should_NotRetry_When_TheExceptionIsNotABaseException()
    {
        // Arrange
        var inner = new PlainExceptionHandler { Throws = static () => new InvalidOperationException(message: "boom") };
        var decorator = new RetryDecorator.DomainEventHandler<PlainExceptionEvent>(innerHandler: inner);

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            actual: async () => await decorator.HandleAsync(@event: new PlainExceptionEvent(), cancellationToken: CancellationToken.None));

        // Assert
        inner.Calls.ShouldBe(
            expected: 1,
            customMessage: "only a retriable BaseException qualifies; anything else goes straight to the outbox's durable retry");
    }

    private static Func<Exception> Retriable =>
        static () => new TransientFailureException(message: "blip", correlationId: null, inner: null);

    private sealed record UnmarkedEvent : TestEventBase;

    private sealed record AlwaysFailingEvent : TestEventBase;

    private sealed record RecoveringEvent : TestEventBase;

    private sealed record NonRetriableEvent : TestEventBase;

    private sealed record PlainExceptionEvent : TestEventBase;

    private abstract record TestEventBase : IDomainEvent
    {
        public Guid Id { get; } = Guid.CreateVersion7();

        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }

    private abstract class CountingHandler<TEvent> : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        public int Calls { get; private set; }

        public int FailuresBeforeSuccess { get; init; } = int.MaxValue;

        public Func<Exception> Throws { get; init; } = static () => new InvalidOperationException();

        public ValueTask HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
        {
            Calls++;

            return Calls > FailuresBeforeSuccess ? ValueTask.CompletedTask : ValueTask.FromException(exception: Throws());
        }
    }

    private sealed class UnmarkedHandler : CountingHandler<UnmarkedEvent>;

    [Retry(3)]
    private sealed class AlwaysFailingHandler : CountingHandler<AlwaysFailingEvent>;

    [Retry(3)]
    private sealed class RecoveringHandler : CountingHandler<RecoveringEvent>;

    [Retry(3)]
    private sealed class NonRetriableHandler : CountingHandler<NonRetriableEvent>;

    [Retry(3)]
    private sealed class PlainExceptionHandler : CountingHandler<PlainExceptionEvent>;
}
