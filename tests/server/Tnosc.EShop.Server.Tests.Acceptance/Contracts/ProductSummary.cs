// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// A product as the catalogue search returns it.
/// </summary>
/// <param name="Id">The product's identifier.</param>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's display name.</param>
/// <param name="PriceAmount">The product's price.</param>
/// <param name="PriceCurrency">The currency the price is denominated in.</param>
public sealed record ProductSummary(
    Guid Id,
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency);
