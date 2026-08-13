// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket;

namespace Tnosc.EShop.Server.Api.Basket.AddItemToBasket;

/// <summary>
/// The body of a request adding a product to the caller's basket.
/// </summary>
/// <param name="ProductId">The identifier of the product to add.</param>
/// <param name="Quantity">The quantity to add.</param>
internal sealed record AddItemToBasketRequest(Guid ProductId, int Quantity)
{
    /// <summary>
    /// Composes the command from this body and the caller's identity.
    /// </summary>
    /// <param name="customerId">The caller's own customer identifier, from their validated token.</param>
    /// <returns>The command to hand to the handler.</returns>
    public AddItemToBasketCommand ToCommand(Guid customerId) =>
        new(CustomerId: customerId, ProductId: ProductId, Quantity: Quantity);
}
