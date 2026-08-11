// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Exceptions;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// Fails the first <see cref="FlakyTestDomainEventPlan.FailuresBeforeSuccess"/> invocations with a
/// retriable exception, then records the delivery into <see cref="TestDomainEventSpy"/>.
/// </summary>
/// <remarks>
/// Carries both attributes deliberately. <c>[Retry(3)]</c> is what this handler exists to exercise;
/// <c>[Idempotent]</c> is there because retrying a handler that is not idempotent can re-apply
/// whatever the failed attempt already committed — the pairing this handler is meant to model.
/// </remarks>
/// <param name="plan">Decides which invocations fail.</param>
/// <param name="spy">The process-wide delivery recorder.</param>
[Retry(3)]
[Idempotent]
internal sealed class FlakyTestDomainEventHandler(FlakyTestDomainEventPlan plan, TestDomainEventSpy spy)
    : IDomainEventHandler<FlakyTestDomainEvent>
{
    /// <inheritdoc />
    public ValueTask HandleAsync(FlakyTestDomainEvent @event, CancellationToken cancellationToken = default)
    {
        if (plan.RecordAndShouldFail())
        {
            throw new TransientFailureException(
                message: $"Flaky message {@event.Id} failed this attempt.",
                correlationId: null,
                inner: null);
        }

        spy.RecordDelivery(aggregateId: @event.AggregateId);

        return ValueTask.CompletedTask;
    }
}
