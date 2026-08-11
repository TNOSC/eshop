// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.DomainEvents;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// The broken half of the fan-out pair: throws while <see cref="FanOutTestPlan.FailingHandlerShouldFail"/>
/// is set, so it exhausts the outbox's attempts and dead-letters.
/// </summary>
/// <remarks>
/// Registered <b>before</b> <see cref="FanOutSucceedingHandler"/> on purpose. Handlers are invoked in
/// registration order, so failing first is what makes the isolation test meaningful — a publisher
/// that stopped at the first throw would never reach the sibling at all.
/// <para>
/// <c>[Idempotent]</c> so its inbox claim rolls back with each failure, which is what leaves the key
/// free for a replay to take.
/// </para>
/// </remarks>
/// <param name="plan">Decides whether this invocation throws.</param>
[Idempotent]
internal sealed class FanOutFailingHandler(FanOutTestPlan plan) : IDomainEventHandler<FanOutTestDomainEvent>
{
    /// <inheritdoc />
    public ValueTask HandleAsync(FanOutTestDomainEvent @event, CancellationToken cancellationToken = default)
    {
        if (plan.RecordFailingInvocation())
        {
            throw new InvalidOperationException(message: $"Fan-out handler refuses event {@event.Id}.");
        }

        return ValueTask.CompletedTask;
    }
}
