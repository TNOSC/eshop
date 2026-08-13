// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderById;

/// <summary>
/// One line of an order, as the query side returns it.
/// </summary>
/// <param name="Id">The line's identifier.</param>
/// <param name="ProductId">The identifier of the product ordered.</param>
/// <param name="Sku">The product's stock-keeping unit at order time.</param>
/// <param name="ProductName">The product's display name at order time.</param>
/// <param name="UnitPriceAmount">The unit price the customer paid.</param>
/// <param name="UnitPriceCurrency">The three-letter ISO 4217 currency of the unit price.</param>
/// <param name="Quantity">The number of units ordered.</param>
/// <param name="LineTotalAmount">What the line cost — unit price times quantity.</param>
public sealed record OrderLineDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    int Quantity,
    decimal LineTotalAmount);
