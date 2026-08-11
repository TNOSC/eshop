// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Domain;

/// <summary>
/// Represents the entity identifier interface.
/// </summary>
public interface IEntityId;

/// <summary>
/// Represents a strongly-typed entity identifier that exposes its underlying value.
/// </summary>
/// <typeparam name="TValue">The type of the underlying value. Must be non-nullable.</typeparam>
public interface IEntityId<out TValue> : IEntityId
    where TValue : notnull
{
    /// <summary>
    /// Gets the underlying value of the identifier.
    /// </summary>
    TValue Value { get; }
}

/// <summary>
/// Represents a strongly-typed entity identifier that can be constructed from its underlying value
/// without reflection.
/// </summary>
/// <typeparam name="TSelf">The identifier type implementing this interface.</typeparam>
/// <typeparam name="TValue">The type of the underlying value. Must be non-nullable.</typeparam>
public interface IEntityId<TSelf, TValue> : IEntityId<TValue>
    where TSelf : class, IEntityId<TSelf, TValue>
    where TValue : notnull
{
    /// <summary>
    /// Creates an instance of <typeparamref name="TSelf"/> from its underlying value.
    /// </summary>
    /// <param name="value">The underlying value.</param>
    /// <returns>A new instance of <typeparamref name="TSelf"/> wrapping <paramref name="value"/>.</returns>
    static abstract TSelf From(TValue value);
}
