// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderById;

/// <summary>
/// A single order with its lines, as the query side returns it.
/// </summary>
/// <param name="Id">The order's identifier.</param>
/// <param name="OrderNumber">The order's human-facing reference.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="Status">The order's status, as its name.</param>
/// <param name="TotalAmount">The order's total after any discount.</param>
/// <param name="TotalCurrency">The three-letter ISO 4217 currency of the total.</param>
/// <param name="PlacedOnUtc">The UTC instant the order was placed.</param>
/// <param name="ShippingStreet">The street line of the delivery address.</param>
/// <param name="ShippingCity">The city of the delivery address.</param>
/// <param name="ShippingPostalCode">The postal code of the delivery address.</param>
/// <param name="ShippingCountry">The ISO 3166-1 alpha-2 country code of the delivery address.</param>
/// <param name="Lines">The order's lines.</param>
public sealed record OrderDto(
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
    IReadOnlyCollection<OrderLineDto> Lines);
