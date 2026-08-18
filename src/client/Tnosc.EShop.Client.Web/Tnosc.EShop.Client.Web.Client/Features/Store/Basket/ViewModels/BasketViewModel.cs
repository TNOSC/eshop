// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Basket.ViewModels;

/// <summary>
/// The caller's basket, as shown by <c>BasketPage</c>. Read-only display data mapped from
/// <see cref="Tnosc.EShop.Client.Web.Contracts.Basket.Basket"/> by <c>BasketPageService</c> — not a
/// form, so it carries no DataAnnotations.
/// </summary>
public sealed class BasketViewModel
{
    /// <summary>Gets or sets the basket's lines.</summary>
    public IReadOnlyList<BasketItemViewModel> Items { get; init; } = [];

    /// <summary>Gets or sets the basket's total amount.</summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>Gets or sets the basket's total currency.</summary>
    public string? TotalCurrency { get; init; }
}
