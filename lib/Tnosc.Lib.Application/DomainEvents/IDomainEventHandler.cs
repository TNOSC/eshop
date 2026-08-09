// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
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
