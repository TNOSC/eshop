// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Catalog;

/// <summary>A single product row as shown in a catalog listing.</summary>
public sealed record ProductSummary(
    Guid Id,
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int StockQuantity,
    string BrandName,
    string CategoryName);
