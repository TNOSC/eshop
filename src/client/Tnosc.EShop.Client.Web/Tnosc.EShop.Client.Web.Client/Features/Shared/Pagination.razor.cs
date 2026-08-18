// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Components;

namespace Tnosc.EShop.Client.Web.Client.Features.Shared;

/// <summary>
/// The page-number strip shared by every server-paged list page (<c>Products</c>, <c>MyOrders</c>).
/// Purely presentational — the caller owns the <see cref="Microsoft.FluentUI.AspNetCore.Components.PaginationState"/>
/// and how a page index becomes a URI.
/// </summary>
public partial class Pagination : ComponentBase
{
    /// <summary>Gets or sets the zero-based index of the currently displayed page.</summary>
    [Parameter]
    [EditorRequired]
    public int CurrentPageIndex { get; set; }

    /// <summary>Gets or sets the zero-based index of the last available page.</summary>
    [Parameter]
    [EditorRequired]
    public int? LastPageIndex { get; set; }

    /// <summary>Gets or sets the function building the link for a given zero-based page index.</summary>
    [Parameter]
    [EditorRequired]
    public Func<int, string> PageUriFactory { get; set; } = null!;
}
