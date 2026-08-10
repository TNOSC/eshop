// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Catalog.Products.Events;

/// <summary>
/// Raised when a product's price changes.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="ProductId">The identifier of the repriced product.</param>
/// <param name="OldAmount">The price amount before the change.</param>
/// <param name="OldCurrency">The price currency before the change.</param>
/// <param name="NewAmount">The price amount after the change.</param>
/// <param name="NewCurrency">The price currency after the change.</param>
[DomainEventName("catalog.product-price-changed.v1")]
public sealed record ProductPriceChangedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProductId,
    decimal OldAmount,
    string OldCurrency,
    decimal NewAmount,
    string NewCurrency) : IDomainEvent;
