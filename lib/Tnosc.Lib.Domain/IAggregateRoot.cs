// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Domain;

/// <summary>
/// Represents the root entity of an aggregate in a domain-driven design context.
/// </summary>
/// <remarks>An aggregate root is responsible for maintaining the consistency of changes within the aggregate. All
/// access to members of the aggregate should be routed through the aggregate root to enforce invariants and encapsulate
/// business rules.</remarks>
public interface IAggregateRoot : IEntity;

/// <summary>
/// Represents an aggregate root entity with a strongly typed identifier in a domain-driven design context.
/// </summary>
/// <remarks>Aggregate roots are the entry point for modifying and retrieving related entities within an
/// aggregate. This interface is typically used to enforce domain consistency and encapsulate business rules at the
/// aggregate level.</remarks>
/// <typeparam name="TEntityId">The type of the unique identifier for the aggregate root entity. Must implement <see cref="IEntityId"/>.</typeparam>
public interface IAggregateRoot<out TEntityId> : IAggregateRoot, IEntity<TEntityId>
    where TEntityId : class, IEntityId;
