// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tnosc.EShop.Client.Web.Contracts.Catalog;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog;

/// <summary>
/// The <see cref="CreateProductDialog"/>'s form model. <see cref="BrandId"/> is a raw GUID string
/// because there is no brands endpoint yet to populate a <c>FluentSelect</c> from — see the dialog's
/// remarks.
/// </summary>
public sealed class CreateProductModel : IValidatableObject
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

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(value: BrandId) && !Guid.TryParse(input: BrandId, result: out _))
        {
            yield return new ValidationResult(
                errorMessage: "The brand id must be a valid GUID.",
                memberNames: [nameof(BrandId)]);
        }
    }

    /// <summary>Maps this form model to the request body sent to the API.</summary>
    public CreateProductRequest ToRequest() => new(
        Sku: Sku,
        Name: Name,
        Description: string.IsNullOrWhiteSpace(value: Description) ? null : Description,
        PriceAmount: PriceAmount,
        PriceCurrency: PriceCurrency,
        StockQuantity: StockQuantity,
        BrandId: Guid.Parse(input: BrandId),
        CategoryId: CategoryId!.Value);
}
