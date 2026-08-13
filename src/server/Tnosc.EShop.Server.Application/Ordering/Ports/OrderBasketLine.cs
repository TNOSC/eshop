// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Ordering.Ports;

/// <summary>
/// One line of the basket an order is being placed from.
/// </summary>
/// <param name="ProductId">The identifier of the product on this line.</param>
/// <param name="Sku">The product's stock-keeping unit, as the basket snapshotted it.</param>
/// <param name="ProductName">The product's display name, as the basket snapshotted it.</param>
/// <param name="UnitPriceAmount">The unit price the customer saw when they added the line.</param>
/// <param name="UnitPriceCurrency">The three-letter ISO 4217 currency of the unit price.</param>
/// <param name="Quantity">The number of units on the line.</param>
public sealed record OrderBasketLine(
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    int Quantity);
