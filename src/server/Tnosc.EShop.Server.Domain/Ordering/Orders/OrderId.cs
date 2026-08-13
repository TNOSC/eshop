// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Strongly-typed identifier for <see cref="Order"/>.
/// </summary>
public sealed record OrderId : GuidEntityId, IEntityId<OrderId, Guid>
{
    private OrderId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new identifier backed by a time-ordered (version 7) <see cref="Guid"/>, so inserts
    /// stay sequential and the primary-key index does not fragment.
    /// </summary>
    /// <returns>A new <see cref="OrderId"/>.</returns>
    public static OrderId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static OrderId From(Guid value) => new(value);
}
