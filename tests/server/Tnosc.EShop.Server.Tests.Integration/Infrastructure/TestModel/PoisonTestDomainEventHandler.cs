// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.DomainEvents;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// Always-throws handler for <see cref="PoisonTestDomainEvent"/>, used to exercise the outbox
/// processor's failure, backoff, and dead-lettering behaviour.
/// </summary>
internal sealed class PoisonTestDomainEventHandler : IDomainEventHandler<PoisonTestDomainEvent>
{
    /// <inheritdoc />
    public ValueTask HandleAsync(PoisonTestDomainEvent @event, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(message: $"Poison message {@event.Id} always fails.");
}
