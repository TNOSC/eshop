// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Strongly-typed identifier for <see cref="OrderLine"/>.
/// </summary>
public sealed record OrderLineId : GuidEntityId, IEntityId<OrderLineId, Guid>
{
    private OrderLineId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new identifier backed by a time-ordered (version 7) <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="OrderLineId"/>.</returns>
    public static OrderLineId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static OrderLineId From(Guid value) => new(value);
}
