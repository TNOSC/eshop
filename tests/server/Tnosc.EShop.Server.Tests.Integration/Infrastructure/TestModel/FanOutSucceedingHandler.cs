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
/// The healthy half of the fan-out pair: always succeeds, recording the delivery into
/// <see cref="TestDomainEventSpy"/>.
/// </summary>
/// <remarks>
/// <c>[Idempotent]</c> is what makes it run exactly once even though its broken sibling forces the
/// message to be redelivered — its inbox claim is committed on the first pass, so every later pass
/// skips it. Without the attribute it would re-run on every retry, which is the at-least-once
/// behaviour the inbox exists to close.
/// </remarks>
/// <param name="plan">Records the invocation count.</param>
/// <param name="spy">The process-wide delivery recorder.</param>
[Idempotent]
internal sealed class FanOutSucceedingHandler(FanOutTestPlan plan, TestDomainEventSpy spy)
    : IDomainEventHandler<FanOutTestDomainEvent>
{
    /// <inheritdoc />
    public ValueTask HandleAsync(FanOutTestDomainEvent @event, CancellationToken cancellationToken = default)
    {
        plan.RecordSucceedingInvocation();
        spy.RecordDelivery(aggregateId: @event.AggregateId);

        return ValueTask.CompletedTask;
    }
}
