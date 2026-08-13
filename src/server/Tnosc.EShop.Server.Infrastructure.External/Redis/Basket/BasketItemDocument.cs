// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// The serialized shape of a single <c>BasketItem</c> line inside a <see cref="BasketDocument"/>.
/// </summary>
/// <param name="ItemId">The line's identifier.</param>
/// <param name="ProductId">The snapshotted product identifier.</param>
/// <param name="Sku">The snapshotted stock-keeping unit.</param>
/// <param name="ProductName">The snapshotted product name.</param>
/// <param name="UnitPriceAmount">The snapshotted unit price amount.</param>
/// <param name="UnitPriceCurrency">The snapshotted unit price currency.</param>
/// <param name="Quantity">The line's quantity.</param>
internal sealed record BasketItemDocument(
    Guid ItemId,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    int Quantity);
