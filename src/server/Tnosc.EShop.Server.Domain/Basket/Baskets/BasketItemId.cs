// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// Strongly-typed identifier for <see cref="BasketItem"/>.
/// </summary>
public sealed record BasketItemId : GuidEntityId, IEntityId<BasketItemId, Guid>
{
    private BasketItemId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new identifier backed by a time-ordered (version 7) <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="BasketItemId"/>.</returns>
    public static BasketItemId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static BasketItemId From(Guid value) => new(value);
}
