// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Represents the base type for strongly-typed entity identifiers backed by a <see cref="Guid"/>.
/// </summary>
/// <remarks>Derive a sealed record from this type and additionally implement
/// <see cref="IEntityId{TSelf, TValue}"/> with <c>TSelf</c> set to the derived type, so callers can
/// construct instances through <c>From</c> without reflection.</remarks>
public abstract record GuidEntityId : IEntityId<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidEntityId"/> class.
    /// </summary>
    /// <param name="value">The underlying <see cref="Guid"/> value.</param>
    protected GuidEntityId(Guid value) => Value = value;

    /// <inheritdoc />
    public Guid Value { get; }

    /// <summary>
    /// Returns the string representation of the underlying <see cref="Guid"/> value.
    /// </summary>
    /// <returns>The string representation of <see cref="Value"/>.</returns>
    public sealed override string ToString() => Value.ToString();
}
