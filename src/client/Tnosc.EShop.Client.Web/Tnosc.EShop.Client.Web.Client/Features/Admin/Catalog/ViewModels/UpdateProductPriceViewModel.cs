// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;

/// <summary>The update-product-price dialog's ViewModel.</summary>
public sealed class UpdateProductPriceViewModel
{
    /// <summary>Gets or sets the new price amount.</summary>
    [Range(minimum: 0, maximum: double.MaxValue, ErrorMessage = "The amount cannot be negative.")]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the new price currency, as a three-letter ISO 4217 code.</summary>
    [Required]
    public string Currency { get; set; } = string.Empty;
}
