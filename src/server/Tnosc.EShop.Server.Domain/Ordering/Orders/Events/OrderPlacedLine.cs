// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders.Events;

/// <summary>
/// One line of an <see cref="OrderPlacedDomainEvent"/> payload.
/// </summary>
/// <remarks>
/// Flat primitives, like every other part of an event payload — a <see cref="Guid"/> product id, not
/// an <c>OrderLineId</c> or a <c>ProductId</c>. This is what Catalog's stock handler reads to know
/// what to decrement, and it is deserialized by code that may have moved on several versions from the
/// aggregate that wrote it, so nothing here may depend on a domain type staying put.
/// </remarks>
/// <param name="ProductId">The identifier of the product ordered on this line.</param>
/// <param name="Sku">The product's stock-keeping unit at order time.</param>
/// <param name="Quantity">The number of units ordered.</param>
/// <param name="UnitPriceAmount">The unit price the customer paid.</param>
/// <param name="UnitPriceCurrency">The three-letter ISO 4217 currency of the unit price.</param>
public sealed record OrderPlacedLine(
    Guid ProductId,
    string Sku,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency);
