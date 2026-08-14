// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Contracts.Ordering;

/// <summary>The full detail of a single placed order. <see cref="Status"/> is the server's plain
/// wire string — do not translate it into a client-side enum.</summary>
public sealed record Order(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string TotalCurrency,
    DateTime PlacedOnUtc,
    string ShippingStreet,
    string ShippingCity,
    string ShippingPostalCode,
    string ShippingCountry,
    IReadOnlyList<OrderLine> Lines);
