// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Conventions;

/// <summary>
/// Converts between a strongly-typed entity id and its underlying primitive value for EF Core mapping.
/// </summary>
/// <remarks>
/// Built on <see cref="IEntityId{TSelf, TValue}"/>'s <c>static abstract From</c>, so no reflection is
/// needed to construct <typeparamref name="TId"/> from its stored value.
/// </remarks>
/// <typeparam name="TId">The strongly-typed entity id.</typeparam>
/// <typeparam name="TValue">The underlying primitive value type.</typeparam>
public sealed class EntityIdConverter<TId, TValue> : ValueConverter<TId, TValue>
    where TId : class, IEntityId<TId, TValue>
    where TValue : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityIdConverter{TId, TValue}"/> class.
    /// </summary>
    public EntityIdConverter() : base(id => id.Value, value => FromValue(value))
    {
    }

    // Expression trees cannot reference a static abstract interface member directly (CS8927);
    // routing the call through an ordinary static method sidesteps that restriction.
    private static TId FromValue(TValue value) => TId.From(value);
}
