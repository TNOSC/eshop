// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

/// <summary>
/// The storefront product gallery's ViewModel: the current search/filter/page state sent to the
/// component service's <c>SearchAsync</c>. The loaded results and lifecycle state are the
/// component's own — see <c>blazor-client-mvvm</c> — not part of this ViewModel. Not a form, so it
/// carries no DataAnnotations — those are opt-in per the rule, not mandatory on every ViewModel.
/// </summary>
public sealed class ProductsViewModel
{
    /// <summary>Gets or sets the free-text search term.</summary>
    public string? Search { get; set; }

    /// <summary>Gets or sets the selected category filter.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the one-based requested page.</summary>
    public int Page { get; set; } = 1;
}
