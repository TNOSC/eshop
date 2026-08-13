// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// Every failure a caller can get back about a <see cref="Basket"/>, defined once.
/// </summary>
public static class BasketErrors
{
    /// <summary>
    /// No line in the basket carries the requested item identifier.
    /// </summary>
    /// <param name="itemId">The item identifier that was looked up.</param>
    public static Error ItemNotFound(Guid itemId) => Error.NotFound(
        code: "Basket.ItemNotFound",
        description: $"Basket item {itemId} was not found.");

    /// <summary>
    /// The product a caller tried to add to (or look up for) a basket does not exist in the
    /// catalogue.
    /// </summary>
    /// <param name="productId">The identifier that was looked up.</param>
    public static Error ProductNotFound(Guid productId) => Error.NotFound(
        code: "Basket.ProductNotFound",
        description: $"Product {productId} was not found.");
}
