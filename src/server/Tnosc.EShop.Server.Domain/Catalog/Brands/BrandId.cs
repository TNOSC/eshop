// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Catalog.Brands;

/// <summary>
/// Strongly-typed identifier for <see cref="Brand"/>.
/// </summary>
public sealed record BrandId : GuidEntityId, IEntityId<BrandId, Guid>
{
    private BrandId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new identifier backed by a time-ordered (version 7) <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="BrandId"/>.</returns>
    public static BrandId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static BrandId From(Guid value) => new(value);
}
