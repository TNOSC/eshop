// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Catalog;

/// <summary>A single product row as shown in a catalog listing.</summary>
#pragma warning disable CA1054 // ImageUrl is a flat wire-format string like every other field here, never System.Uri.
public sealed record ProductSummary(
    Guid Id,
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int StockQuantity,
    string BrandName,
    string CategoryName,
    string? ImageUrl);
#pragma warning restore CA1054
