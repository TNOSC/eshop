// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;

/// <summary>
/// Maps a server error code to the <see cref="CreateProductModel"/> field it belongs to.
/// <c>CustomResults</c> on the server keys its <c>errors</c> dictionary by error code, not field
/// name, so <c>ValidationMessage</c> cannot resolve one on its own — this bridges the two vocabularies.
/// A code with no entry here is not a bug: the caller falls back to showing it in a message bar
/// instead of silently dropping it.
/// </summary>
internal static class ValidationCodeFieldMap
{
    private static readonly FrozenDictionary<string, string> CodeToField =
        new Dictionary<string, string>(comparer: StringComparer.Ordinal)
        {
            ["Sku.Empty"] = nameof(CreateProductModel.Sku),
            ["Sku.TooLong"] = nameof(CreateProductModel.Sku),
            ["Sku.InvalidFormat"] = nameof(CreateProductModel.Sku),
            ["Product.SkuAlreadyExists"] = nameof(CreateProductModel.Sku),
            ["Product.NameRequired"] = nameof(CreateProductModel.Name),
            ["Product.NameTooLong"] = nameof(CreateProductModel.Name),
            ["Product.DescriptionTooLong"] = nameof(CreateProductModel.Description),
            ["Product.BrandRequired"] = nameof(CreateProductModel.BrandId),
            ["Brand.NotFound"] = nameof(CreateProductModel.BrandId),
            ["Product.CategoryRequired"] = nameof(CreateProductModel.CategoryId),
            ["Category.NotFound"] = nameof(CreateProductModel.CategoryId),
            ["Money.NegativeAmount"] = nameof(CreateProductModel.PriceAmount),
            ["Money.InvalidCurrency"] = nameof(CreateProductModel.PriceCurrency),
            ["StockQuantity.Negative"] = nameof(CreateProductModel.StockQuantity),
        }.ToFrozenDictionary(comparer: StringComparer.Ordinal);

    /// <summary>Resolves a server error code to the form field it applies to.</summary>
    /// <param name="errorCode">The error code from the problem's <c>errors</c> dictionary.</param>
    /// <param name="fieldName">The resolved field name, when found.</param>
    public static bool TryResolveField(string errorCode, [NotNullWhen(true)] out string? fieldName) =>
        CodeToField.TryGetValue(key: errorCode, value: out fieldName);
}
