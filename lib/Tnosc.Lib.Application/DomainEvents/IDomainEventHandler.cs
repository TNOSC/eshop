// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Application.DomainEvents;

/// <summary>
/// Handler interface for processing domain events.
/// Multiple handlers can subscribe to the same domain event type.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle</typeparam>
/// <remarks>
/// <para>
/// Outbox delivery is <b>at-least-once</b>: a crash between publishing and marking the outbox
/// message processed replays it, so implementations must be idempotent. Dedupe on
/// <see cref="IDomainEvent.Id"/> — a stable <see cref="System.Guid"/> assigned at construction and
/// serialized into the outbox payload, so it survives the replay and identifies the message
/// uniquely.
/// </para>
/// <para>
/// A handler that cannot reasonably be made idempotent by hand marks itself
/// <see cref="Attributes.IdempotentAttribute"/> instead. That is the inbox this contract was
/// designed for and it needed no change here, exactly because <see cref="IDomainEvent.Id"/> was
/// already durable and stable: <c>IdempotencyDecorator</c> claims the id for the handler in the same
/// transaction as the handler's own writes, so a redelivered event is skipped rather than applied
/// twice — and a handler that crashed part-way rolls the claim back with its work and is retried
/// properly.
/// </para>
/// </remarks>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event asynchronously.
    /// </summary>
    /// <param name="event">The domain event instance containing event data</param>
    /// <param name="cancellationToken">
    /// Cancellation token that should be observed to allow graceful cancellation.
    /// Note: If this handler fails or is cancelled, other handlers will still execute.
    /// </param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
