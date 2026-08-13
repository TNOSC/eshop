// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Represents the base class for aggregate roots in a domain-driven design context, providing support for domain events
/// and versioning.
/// </summary>
/// <remarks>Aggregate roots serve as the entry point for modifying and retrieving the state of an aggregate. This
/// class manages domain events associated with the aggregate and tracks its version for optimistic concurrency control.
/// Inherit from this class to implement specific aggregate root behavior in your domain model.</remarks>
public abstract class AggregateRoot<TEntityId> : Entity<TEntityId>, IAggregateRoot<TEntityId>, IAuditable, IHasDomainEvent
    where TEntityId : class, IEntityId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the current version number used for optimistic concurrency control.
    /// </summary>
    /// <remarks>The version is typically incremented each time the entity is updated. It can be used to
    /// detect conflicting changes when multiple processes attempt to modify the same entity concurrently.</remarks>
    public int Version { get; private set; } // Optimistic concurrency

    /// <summary>
    /// Gets a read-only collection of domain events that have been raised by the entity.
    /// </summary>
    /// <remarks>Use this collection to access domain events for processing or dispatching. The collection is
    /// read-only and reflects the current set of events associated with the entity.</remarks>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// gets the date and time when the entity was created.
    /// </summary>
    public DateTime CreatedOnUtc { get; protected set; }

    /// <summary>
    /// Gets the date and time, in Coordinated Universal Time (UTC), when the entity was last updated.
    /// </summary>
    public DateTime UpdatedOnUtc { get; protected set; }

    /// <summary>
    /// Gets the identifier of the user who created the entity.
    /// </summary>
    public string CreatedBy { get; protected set; } = null!;

    /// <summary>
    /// Gets the identifier of the user who updated the entity.
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Adds a domain event to the collection of events associated with the current entity.
    /// </summary>
    /// <remarks>Domain events are typically used to capture significant changes or actions within the entity
    /// that may be handled by other parts of the system. This method is intended to be used within aggregate root
    /// entities to track events for later processing.</remarks>
    /// <param name="domainEvent">The domain event to add. Cannot be null.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(item: domainEvent);

    /// <summary>
    /// Removes all domain events from the current entity.
    /// </summary>
    /// <remarks>Call this method to clear the list of domain events after they have been processed or
    /// dispatched. This is typically used to prevent duplicate handling of events.</remarks>
    public void ClearDomainEvents() =>
        _domainEvents.Clear();

    /// <summary>
    /// Increments the version number of the entity to reflect a state change.
    /// </summary>
    protected void IncrementVersion() =>
        Version++;

    /// <summary>
    /// Sets the version number directly, bypassing the normal increment-on-mutation path.
    /// </summary>
    /// <remarks>
    /// For aggregates reconstructed from a stored snapshot outside EF's change tracker — for example,
    /// a <c>Basket</c> read back from its Redis document — where the version must be restored exactly
    /// as persisted rather than recomputed by replaying transitions. A call to this method represents
    /// reconstruction, not a state transition: no domain event should be raised alongside it.
    /// </remarks>
    /// <param name="version">The version to restore.</param>
    protected void SetVersion(int version) =>
        Version = version;
}
