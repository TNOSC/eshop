// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Ordering;

/// <summary>A single order row as shown in an order-history listing. <see cref="Status"/> is the
/// server's plain wire string — do not translate it into a client-side enum.</summary>
public sealed record OrderSummary(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalAmount,
    string TotalCurrency,
    DateTime PlacedOnUtc,
    int LineCount);
