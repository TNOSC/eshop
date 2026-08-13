// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Basket.Ports;

/// <summary>
/// A single line of a basket, as returned by <see cref="IBasketReader"/>.
/// </summary>
/// <param name="ItemId">The line's identifier.</param>
/// <param name="ProductId">The identifier of the snapshotted product.</param>
/// <param name="Sku">The snapshotted stock-keeping unit.</param>
/// <param name="ProductName">The snapshotted product name.</param>
/// <param name="UnitPriceAmount">The snapshotted unit price amount.</param>
/// <param name="UnitPriceCurrency">The snapshotted unit price currency.</param>
/// <param name="Quantity">The line's quantity.</param>
public sealed record BasketLineSnapshot(
    Guid ItemId,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    int Quantity);
