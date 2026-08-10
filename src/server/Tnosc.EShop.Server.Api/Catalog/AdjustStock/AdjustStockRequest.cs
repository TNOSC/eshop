// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Catalog.Commands.AdjustStock;

namespace Tnosc.EShop.Server.Api.Catalog.AdjustStock;

/// <summary>
/// The HTTP body of <c>POST /api/catalog/products/{id}/stock</c>.
/// </summary>
/// <param name="Delta">The signed number of units to add or remove.</param>
internal sealed record AdjustStockRequest(int Delta)
{
    /// <summary>
    /// Maps this request and its route identifier onto the application command they carry.
    /// </summary>
    /// <param name="productId">The identifier taken from the route.</param>
    /// <returns>The equivalent <see cref="AdjustStockCommand"/>.</returns>
    public AdjustStockCommand ToCommand(Guid productId) =>
        new(ProductId: productId, Delta: Delta);
}
