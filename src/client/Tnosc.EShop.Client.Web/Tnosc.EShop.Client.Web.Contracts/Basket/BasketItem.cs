// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Basket;

/// <summary>A single line in a customer's basket.</summary>
public sealed record BasketItem(
    Guid ItemId,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    int Quantity);
