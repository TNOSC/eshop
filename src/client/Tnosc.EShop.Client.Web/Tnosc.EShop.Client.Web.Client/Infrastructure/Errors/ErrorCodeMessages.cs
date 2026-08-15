// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Tnosc.EShop.Client.Web.Contracts.Common;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;

/// <summary>
/// Maps a server error code (<see cref="ApiProblem.Title"/>) to human-readable text. The codes are a
/// machine vocabulary — <c>Product.NotFound</c> — and are never shown to a shopper directly.
/// </summary>
public static class ErrorCodeMessages
{
    private static readonly FrozenDictionary<string, string> Messages = new Dictionary<string, string>(comparer: StringComparer.Ordinal)
    {
        ["Product.NotFound"] = "That product could not be found.",
        ["Product.SkuAlreadyExists"] = "A product with that SKU already exists.",
        ["Product.Discontinued"] = "That product has been discontinued.",
        ["Product.AlreadyDiscontinued"] = "That product is already discontinued.",
        ["Product.NameRequired"] = "The product name is required.",
        ["Product.NameTooLong"] = "The product name is too long.",
        ["Product.DescriptionTooLong"] = "The product description is too long.",
        ["Product.BrandRequired"] = "A brand must be selected.",
        ["Product.CategoryRequired"] = "A category must be selected.",
        ["Product.StockDeltaRequired"] = "A non-zero stock adjustment is required.",
        ["Category.NotFound"] = "That category could not be found.",
        ["Category.NameRequired"] = "The category name is required.",
        ["Brand.NotFound"] = "That brand could not be found.",
        ["Brand.NameRequired"] = "The brand name is required.",
        ["Sku.Empty"] = "The SKU is required.",
        ["Sku.TooLong"] = "The SKU is too long.",
        ["Sku.InvalidFormat"] = "The SKU format is invalid.",
        ["Money.NegativeAmount"] = "The amount cannot be negative.",
        ["Money.InvalidCurrency"] = "The currency is invalid.",
        ["StockQuantity.Negative"] = "The stock quantity cannot be negative.",
    }.ToFrozenDictionary(comparer: StringComparer.Ordinal);

    /// <summary>Resolves a problem to human-readable text.</summary>
    /// <param name="problem">The problem returned by a failed API call.</param>
    public static string Humanize(ApiProblem problem) =>
        Messages.TryGetValue(key: problem.Title ?? string.Empty, value: out string? text)
            ? text
            : problem.Detail ?? "Something went wrong.";
}
