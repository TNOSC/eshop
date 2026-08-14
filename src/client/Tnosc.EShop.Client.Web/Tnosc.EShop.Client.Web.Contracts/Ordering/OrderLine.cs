// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Ordering;

/// <summary>A single line of a placed order.</summary>
public sealed record OrderLine(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    int Quantity,
    decimal LineTotalAmount);
