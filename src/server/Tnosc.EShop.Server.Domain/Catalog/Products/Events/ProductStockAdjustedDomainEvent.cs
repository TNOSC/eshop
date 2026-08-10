// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Catalog.Products.Events;

/// <summary>
/// Raised when a product's stock level is adjusted.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="ProductId">The identifier of the adjusted product.</param>
/// <param name="Delta">The signed number of units added or removed.</param>
/// <param name="NewQuantity">The resulting stock quantity.</param>
[DomainEventName("catalog.product-stock-adjusted.v1")]
public sealed record ProductStockAdjustedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProductId,
    int Delta,
    int NewQuantity) : IDomainEvent;
