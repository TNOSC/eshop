// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// Every way a candidate <see cref="Sku"/> can break the format invariant.
/// </summary>
public static class SkuErrors
{
    /// <summary>
    /// Gets the error returned when no SKU was supplied.
    /// </summary>
    public static Error Empty => Error.Validation(
        code: "Sku.Empty",
        description: "A SKU is required.");

    /// <summary>
    /// Gets the error returned when a SKU exceeds <see cref="Sku.MaxLength"/>.
    /// </summary>
    public static Error TooLong => Error.Validation(
        code: "Sku.TooLong",
        description: $"A SKU must be at most {Sku.MaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when a SKU holds a character outside the allowed set.
    /// </summary>
    public static Error InvalidFormat => Error.Validation(
        code: "Sku.InvalidFormat",
        description: "A SKU may contain only uppercase letters, digits and dashes.");
}
