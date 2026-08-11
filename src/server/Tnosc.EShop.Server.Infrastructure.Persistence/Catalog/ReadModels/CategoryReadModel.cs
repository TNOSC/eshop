// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;

/// <summary>
/// The query-side view of <c>catalog.categories</c>.
/// </summary>
internal sealed class CategoryReadModel : IReadModel
{
    /// <summary>
    /// Gets the category's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the category's display name.
    /// </summary>
    public string Name { get; init; } = null!;
}
