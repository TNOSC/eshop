// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// Every way a candidate <see cref="StockQuantity"/> can break its invariant.
/// </summary>
public static class StockQuantityErrors
{
    /// <summary>
    /// Gets the error returned when a stock level would fall below zero, on creation or adjustment.
    /// </summary>
    public static Error Negative => Error.Validation(
        code: "StockQuantity.Negative",
        description: "A stock quantity must be greater than or equal to zero.");
}
