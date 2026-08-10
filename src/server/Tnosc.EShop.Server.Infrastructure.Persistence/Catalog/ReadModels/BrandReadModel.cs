// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;

/// <summary>
/// The query-side view of <c>catalog.brands</c>.
/// </summary>
internal sealed class BrandReadModel : IReadModel
{
    /// <summary>
    /// Gets the brand's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the brand's display name.
    /// </summary>
    public string Name { get; init; } = null!;
}
