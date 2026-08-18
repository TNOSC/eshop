// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

/// <summary>
/// A single catalog category, as shown by <c>ProductFilters</c>. Read-only display data mapped from
/// <see cref="Tnosc.EShop.Client.Web.Contracts.Catalog.Category"/> by <c>ProductsService</c> — not a
/// form, so it carries no DataAnnotations.
/// </summary>
public sealed class CategoryViewModel
{
    /// <summary>Gets or sets the category id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the category name.</summary>
    public string Name { get; init; } = string.Empty;
}
