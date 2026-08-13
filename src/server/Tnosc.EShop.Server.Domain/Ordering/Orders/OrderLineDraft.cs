// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// One line's worth of raw input to <see cref="Order.Create"/>, before the domain has validated it.
/// </summary>
/// <remarks>
/// Flat primitives on purpose. The caller — <c>OrderInitializer</c>, one workflow step — copies the
/// basket snapshot straight across and hands the whole set to the aggregate; every check on the
/// numbers (currency shape, non-negative price, quantity bounds, one currency across the order)
/// happens inside <see cref="Order.Create"/>. Handing the step value objects to build instead would
/// move that validation into the application layer one <c>Result</c> at a time, which is precisely
/// what the rich-domain rule is there to prevent.
/// </remarks>
/// <param name="ProductId">The identifier of the catalogue product being ordered.</param>
/// <param name="Sku">The product's stock-keeping unit, snapshotted at order time.</param>
/// <param name="ProductName">The product's display name, snapshotted at order time.</param>
/// <param name="UnitPriceAmount">The unit price the customer is paying.</param>
/// <param name="UnitPriceCurrency">The three-letter ISO 4217 currency of the unit price.</param>
/// <param name="Quantity">The number of units ordered.</param>
public sealed record OrderLineDraft(
    Guid ProductId,
    string? Sku,
    string? ProductName,
    decimal UnitPriceAmount,
    string? UnitPriceCurrency,
    int Quantity);
