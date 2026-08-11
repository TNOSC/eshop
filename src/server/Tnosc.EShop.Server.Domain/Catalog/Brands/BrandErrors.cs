// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Brands;

/// <summary>
/// Every failure a caller can get back about a <see cref="Brand"/>, defined once.
/// </summary>
public static class BrandErrors
{
    /// <summary>
    /// No brand carries the requested identifier.
    /// </summary>
    /// <param name="brandId">The identifier that was looked up.</param>
    public static Error NotFound(Guid brandId) => Error.NotFound(
        code: "Brand.NotFound",
        description: $"Brand {brandId} was not found.");

    /// <summary>
    /// Gets the error returned when a brand name is missing.
    /// </summary>
    public static Error NameRequired => Error.Validation(
        code: "Brand.NameRequired",
        description: "A brand name is required.");
}
