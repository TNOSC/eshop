// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Domain;

/// <summary>
/// Defines a contract for entities that can be identified or managed within a domain or data model.
/// </summary>
/// <remarks>Implement this interface to indicate that a type represents a domain entity. The specific
/// requirements or members for entities are defined by derived interfaces or implementations.</remarks>
public interface IEntity;

/// <summary>
/// Defines a contract for an entity with a strongly typed identifier.
/// </summary>
/// <remarks>This interface is typically used to provide a consistent way to access the unique identifier of an
/// entity in domain-driven design or persistence scenarios. Implementations should ensure that the <see cref="Id"/>
/// property uniquely identifies the entity instance.</remarks>
/// <typeparam name="TEntityId">The type of the entity identifier. Must implement <see cref="IEntityId"/> and be a reference type.</typeparam>
public interface IEntity<out TEntityId> : IEntity
    where TEntityId : class, IEntityId
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    TEntityId Id { get; }
}
