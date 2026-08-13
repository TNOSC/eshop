// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Basket.Ports;

/// <summary>
/// The catalogue data a basket line snapshots when a product is added, as returned by
/// <see cref="IProductLookup"/>.
/// </summary>
/// <param name="ProductId">The product's identifier.</param>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="PriceAmount">The product's current price amount.</param>
/// <param name="PriceCurrency">The product's current price currency.</param>
public sealed record ProductSnapshot(
    Guid ProductId,
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency);
