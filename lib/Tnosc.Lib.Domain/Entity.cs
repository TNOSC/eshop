// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Represents a base entity with a strongly typed identifier.
/// Provides equality comparison based on the entity's identity rather than reference.
/// </summary>
/// <typeparam name="TEntityId">
/// The type of the entity identifier. Must be non-nullable.
/// </typeparam>
public abstract class Entity<TEntityId> : IEntity<TEntityId>, IEquatable<Entity<TEntityId>>
    where TEntityId : class, IEntityId
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    /// <remarks>
    /// This property is required and must be initialized when creating an entity instance.
    /// </remarks>
    public required TEntityId Id { get; init; }

    /// <summary>
    /// Determines whether two <see cref="Entity{TId}"/> instances are equal.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns>
    /// <see langword="true"/> if both entities are null or represent the same identity; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(Entity<TEntityId>? a, Entity<TEntityId>? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(other: b);
    }

    /// <summary>
    /// Determines whether two <see cref="Entity{TId}"/> instances are not equal.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the entities are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(Entity<TEntityId>? a, Entity<TEntityId>? b)
        => !(a == b);

    /// <summary>
    /// Determines whether the current entity is equal to another entity of the same type.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>
    /// <see langword="true"/> if the entities are of the same type and have the same identifier; otherwise, <c>false</c>.
    /// </returns>
    public virtual bool Equals(Entity<TEntityId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (other.GetType() != GetType())
        {
            return false;
        }

        return ReferenceEquals(objA: this, objB: other) || Id.Equals(obj: other.Id);
    }

    /// <summary>
    /// Determines whether the current entity is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>
    /// <see langword="true"/> if the object is an <see cref="Entity{TId}"/> with the same identifier; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(objA: this, objB: obj))
        {
            return true;
        }

        if (obj is not Entity<TEntityId> other)
        {
            return false;
        }

        return Id.Equals(obj: other.Id);
    }

    /// <summary>
    /// Returns a hash code for the entity based on its identifier.
    /// </summary>
    /// <returns>A hash code for the entity.</returns>
    public override int GetHashCode()
        => HashCode.Combine(value1: Id);
}
