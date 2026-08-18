// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Components;

public partial class ProductFilters : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public string? Search { get; set; }

    [Parameter]
    [EditorRequired]
    public EventCallback<string?> SearchChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public Guid? CategoryId { get; set; }

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<CategoryViewModel> Categories { get; set; } = [];

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    private string CategoryUri(Guid? categoryId) =>
        Navigation.GetUriWithQueryParameters(parameters: new Dictionary<string, object?>(comparer: StringComparer.Ordinal)
        {
            ["page"] = null,
            ["categoryId"] = categoryId,
        });
}
