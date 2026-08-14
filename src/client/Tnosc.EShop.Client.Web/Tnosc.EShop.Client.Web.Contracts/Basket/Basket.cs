// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Contracts.Basket;

/// <summary>The current caller's basket.</summary>
#pragma warning disable MA0049 // Matches the server's DTO name; the "Basket" bounded context folder name is not negotiable, and "BasketDto" would be the odd one out among sibling records.
public sealed record Basket(
#pragma warning restore MA0049
    Guid BasketId,
    Guid CustomerId,
    IReadOnlyList<BasketItem> Items,
    decimal? TotalAmount,
    string? TotalCurrency);
