// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Defines a contract for entities that expose domain events for processing by external handlers.
/// </summary>
/// <remarks>Implement this interface to enable an entity to publish domain events as part of its lifecycle.
/// Domain events represent significant changes or actions within the domain model that may trigger side effects or be
/// handled by other components. This interface is typically used in domain-driven design (DDD) patterns to facilitate
/// event-driven architectures.</remarks>
public interface IHasDomainEvent
{
    /// <summary>
    /// Gets the collection of domain events that have been raised by the entity.
    /// </summary>
    /// <remarks>Domain events represent significant changes or actions within the entity that may be handled
    /// by other parts of the system. The collection is typically cleared after the events have been
    /// dispatched.</remarks>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Removes all domain events currently associated with the entity.
    /// </summary>
    /// <remarks>Call this method to clear the collection of domain events after they have been processed or
    /// dispatched. This is typically used in domain-driven design patterns to prevent events from being handled
    /// multiple times.</remarks>
    void ClearDomainEvents();
}
