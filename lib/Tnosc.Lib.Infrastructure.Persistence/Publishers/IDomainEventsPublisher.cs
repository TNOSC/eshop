// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Publishers;

/// <summary>
/// Interface for publishing domain events to their registered handlers.
/// </summary>
public interface IDomainEventsPublisher
{
    /// <summary>
    /// Publishes domain events to every registered handler, running each handler independently.
    /// </summary>
    /// <remarks>
    /// A handler that throws does <b>not</b> stop the ones after it: every handler is attempted, and
    /// the failures come back in the report. One handler's problem is not a reason to withhold an
    /// event from unrelated handlers.
    /// </remarks>
    /// <param name="domainEvents">The domain events to publish.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A report naming every handler that threw; empty when delivery was clean.</returns>
    ValueTask<DomainEventDeliveryReport> PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes one domain event to one named handler, leaving every other handler of that event
    /// untouched. This is the dead-letter replay path.
    /// </summary>
    /// <remarks>
    /// Scoped to a single handler on purpose: the siblings of a dead-lettered handler have usually
    /// already succeeded, and re-publishing the whole event would re-run any of them that is not
    /// <c>[Idempotent]</c>.
    /// </remarks>
    /// <param name="domainEvent">The domain event to deliver.</param>
    /// <param name="handlerName">The durable handler name, as produced by <see cref="Tnosc.Lib.Application.Decorators.HandlerChain"/>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <exception cref="InvalidOperationException">No handler registered for the event matches <paramref name="handlerName"/>.</exception>
    ValueTask PublishToHandlerAsync(IDomainEvent domainEvent, string handlerName, CancellationToken cancellationToken = default);
}
