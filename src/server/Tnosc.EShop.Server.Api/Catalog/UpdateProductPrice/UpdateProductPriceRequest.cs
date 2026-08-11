// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;

namespace Tnosc.EShop.Server.Api.Catalog.UpdateProductPrice;

/// <summary>
/// The HTTP body of <c>PUT /api/catalog/products/{id}/price</c>.
/// </summary>
/// <param name="Amount">The new price amount.</param>
/// <param name="Currency">The three-letter ISO 4217 currency of the new price.</param>
internal sealed record UpdateProductPriceRequest(decimal Amount, string? Currency)
{
    /// <summary>
    /// Maps this request and its route identifier onto the application command they carry.
    /// </summary>
    /// <param name="productId">The identifier taken from the route.</param>
    /// <returns>The equivalent <see cref="UpdateProductPriceCommand"/>.</returns>
    public UpdateProductPriceCommand ToCommand(Guid productId) =>
        new(ProductId: productId, Amount: Amount, Currency: Currency);
}
