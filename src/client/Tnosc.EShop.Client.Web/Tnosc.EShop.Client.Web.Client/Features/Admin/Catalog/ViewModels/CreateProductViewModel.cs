// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;

/// <summary>
/// The create-product dialog's ViewModel. <see cref="BrandId"/> is a raw GUID string
/// because there is no brands endpoint yet to populate a <c>FluentSelect</c> from — see the dialog's
/// remarks. Validation and mapping to <c>CreateProductRequest</c> live in the dialog's component
/// service, not here — this class only holds bindable state and its DataAnnotations constraints.
/// </summary>
public sealed class CreateProductViewModel
{
    /// <summary>Gets or sets the product's SKU.</summary>
    [Required(ErrorMessage = "The SKU is required.")]
    [StringLength(maximumLength: 64)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the product's name.</summary>
    [Required(ErrorMessage = "The name is required.")]
    [StringLength(maximumLength: 200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the product's description. An empty value is sent as <see langword="null"/>.</summary>
    [StringLength(maximumLength: 2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial price amount.</summary>
    [Range(minimum: 0, maximum: double.MaxValue, ErrorMessage = "The amount cannot be negative.")]
    public decimal PriceAmount { get; set; }

    /// <summary>Gets or sets the initial price currency, as a three-letter ISO 4217 code.</summary>
    [Required]
    public string PriceCurrency { get; set; } = "USD";

    /// <summary>Gets or sets the initial stock quantity.</summary>
    [Range(minimum: 0, maximum: int.MaxValue, ErrorMessage = "The stock quantity cannot be negative.")]
    public int StockQuantity { get; set; }

    /// <summary>Gets or sets the category id. <see langword="null"/> until one is picked.</summary>
    [Required(ErrorMessage = "A category must be selected.")]
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the brand id, entered as text pending a brands endpoint.</summary>
    [Required(ErrorMessage = "A brand id is required.")]
    public string BrandId { get; set; } = string.Empty;
}
