// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Components;

public partial class ProductCard : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public required ProductSummaryViewModel Product { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an "Add to basket" button is shown below the card.
    /// Off by default, so the storefront gallery's existing cards are unaffected — a caller opts in
    /// where the action makes sense, e.g. from the shopping assistant's chat.
    /// </summary>
    [Parameter]
    public bool ShowAddToBasket { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked with <see cref="ViewModels.ProductSummaryViewModel.Id"/>
    /// when "Add to basket" is clicked. The card stays presentational: it raises the request, it does
    /// not call a basket API itself.
    /// </summary>
    [Parameter]
    public EventCallback<Guid> OnAddToBasket { get; set; }

    private Task AddToBasketAsync() => OnAddToBasket.InvokeAsync(arg: Product.Id);
}
