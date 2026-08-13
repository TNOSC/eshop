// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Basket.Commands.ChangeBasketItemQuantity;

namespace Tnosc.EShop.Server.Api.Basket.ChangeBasketItemQuantity;

/// <summary>
/// The body of a request replacing the quantity of one line in the caller's basket.
/// </summary>
/// <param name="Quantity">The new quantity.</param>
internal sealed record ChangeBasketItemQuantityRequest(int Quantity)
{
    /// <summary>
    /// Composes the command from this body, the route's item identifier and the caller's identity.
    /// </summary>
    /// <param name="customerId">The caller's own customer identifier, from their validated token.</param>
    /// <param name="itemId">The identifier of the line to update, from the route.</param>
    /// <returns>The command to hand to the handler.</returns>
    public ChangeBasketItemQuantityCommand ToCommand(Guid customerId, Guid itemId) =>
        new(CustomerId: customerId, ItemId: itemId, Quantity: Quantity);
}
