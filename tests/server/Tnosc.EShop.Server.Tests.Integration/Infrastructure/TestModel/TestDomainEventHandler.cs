// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.DomainEvents;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// Always-succeeds handler for <see cref="TestAggregateCreatedDomainEvent"/>. Records the delivery
/// into <see cref="TestDomainEventSpy"/> so outbox-processor tests can observe which events were
/// actually published, including across two concurrently running processors.
/// </summary>
/// <remarks>
/// <c>[Idempotent]</c> so <c>InboxIdempotencyTests</c> can drive a real redelivery through it. This
/// changes nothing for the other outbox tests: each of their events carries its own
/// <c>IDomainEvent.Id</c> and is delivered once, so every claim is distinct and every delivery still
/// happens.
/// </remarks>
/// <param name="spy">The process-wide delivery recorder.</param>
[Idempotent]
internal sealed class TestDomainEventHandler(TestDomainEventSpy spy) : IDomainEventHandler<TestAggregateCreatedDomainEvent>
{
    /// <inheritdoc />
    public ValueTask HandleAsync(TestAggregateCreatedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        spy.RecordDelivery(aggregateId: @event.AggregateId);
        return ValueTask.CompletedTask;
    }
}
