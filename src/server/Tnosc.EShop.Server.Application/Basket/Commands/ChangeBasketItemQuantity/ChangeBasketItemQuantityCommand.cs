// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Basket.Commands.ChangeBasketItemQuantity;

/// <summary>
/// Replaces the quantity of an existing line in the caller's basket.
/// </summary>
/// <param name="CustomerId">The identifier of the customer whose basket to update.</param>
/// <param name="ItemId">The identifier of the line to update.</param>
/// <param name="Quantity">The new quantity.</param>
public sealed record ChangeBasketItemQuantityCommand(
    Guid CustomerId,
    Guid ItemId,
    int Quantity) : ICommand<BasketDto>;
