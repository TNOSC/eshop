// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;

/// <summary>
/// Maps a server error code to the form field it belongs to. <c>CustomResults</c> on the server keys
/// its <c>errors</c> dictionary by error code, not field name, so <c>ValidationMessage</c> cannot
/// resolve one on its own — this bridges the two vocabularies. A code with no entry here is not a
/// bug: the caller falls back to showing it in a message bar instead of silently dropping it.
/// </summary>
/// <remarks>
/// One flat code-to-field map, shared by every form: the field names below (<c>FirstName</c>,
/// <c>Street</c>, …) are identical across the admin and storefront profile/address ViewModels
/// (<c>CustomerProfileViewModel</c>/<c>CustomerAddressViewModel</c> and
/// <c>MyProfileFormViewModel</c>/<c>MyAddressFormViewModel</c>), so one entry per server error code
/// resolves correctly regardless of which form's <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/>
/// <c>ApplyFieldErrors</c> is called against.
/// </remarks>
internal static class ValidationCodeFieldMap
{
    private static readonly FrozenDictionary<string, string> CodeToField =
        new Dictionary<string, string>(comparer: StringComparer.Ordinal)
        {
            ["Sku.Empty"] = nameof(CreateProductViewModel.Sku),
            ["Sku.TooLong"] = nameof(CreateProductViewModel.Sku),
            ["Sku.InvalidFormat"] = nameof(CreateProductViewModel.Sku),
            ["Product.SkuAlreadyExists"] = nameof(CreateProductViewModel.Sku),
            ["Product.NameRequired"] = nameof(CreateProductViewModel.Name),
            ["Product.NameTooLong"] = nameof(CreateProductViewModel.Name),
            ["Product.DescriptionTooLong"] = nameof(CreateProductViewModel.Description),
            ["Product.BrandRequired"] = nameof(CreateProductViewModel.BrandId),
            ["Brand.NotFound"] = nameof(CreateProductViewModel.BrandId),
            ["Product.CategoryRequired"] = nameof(CreateProductViewModel.CategoryId),
            ["Category.NotFound"] = nameof(CreateProductViewModel.CategoryId),
            ["Money.NegativeAmount"] = nameof(CreateProductViewModel.PriceAmount),
            ["Money.InvalidCurrency"] = nameof(CreateProductViewModel.PriceCurrency),
            ["StockQuantity.Negative"] = nameof(CreateProductViewModel.StockQuantity),
            ["PersonName.FirstNameRequired"] = "FirstName",
            ["PersonName.FirstNameTooLong"] = "FirstName",
            ["PersonName.LastNameRequired"] = "LastName",
            ["PersonName.LastNameTooLong"] = "LastName",
            ["PhoneNumber.Empty"] = "PhoneNumber",
            ["PhoneNumber.InvalidFormat"] = "PhoneNumber",
            ["Address.StreetRequired"] = "Street",
            ["Address.StreetTooLong"] = "Street",
            ["Address.CityRequired"] = "City",
            ["Address.CityTooLong"] = "City",
            ["Address.PostalCodeRequired"] = "PostalCode",
            ["Address.PostalCodeTooLong"] = "PostalCode",
            ["Address.InvalidCountry"] = "Country",
        }.ToFrozenDictionary(comparer: StringComparer.Ordinal);

    /// <summary>Resolves a server error code to the form field it applies to.</summary>
    /// <param name="errorCode">The error code from the problem's <c>errors</c> dictionary.</param>
    /// <param name="fieldName">The resolved field name, when found.</param>
    public static bool TryResolveField(string errorCode, [NotNullWhen(true)] out string? fieldName) =>
        CodeToField.TryGetValue(key: errorCode, value: out fieldName);
}
