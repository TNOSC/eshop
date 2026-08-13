// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket;

/// <summary>
/// Adds a product to the caller's basket, incrementing its line rather than duplicating it when the
/// product is already present.
/// </summary>
/// <param name="CustomerId">The identifier of the customer whose basket to add to.</param>
/// <param name="ProductId">The identifier of the product being added.</param>
/// <param name="Quantity">The quantity being added.</param>
public sealed record AddItemToBasketCommand(
    Guid CustomerId,
    Guid ProductId,
    int Quantity) : ICommand<BasketDto>;
