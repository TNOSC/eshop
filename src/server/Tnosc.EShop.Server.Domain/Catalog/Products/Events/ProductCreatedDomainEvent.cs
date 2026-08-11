// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Catalog.Products.Events;

/// <summary>
/// Raised when a product enters the catalogue.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="ProductId">The identifier of the created product.</param>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Name">The product's name.</param>
/// <param name="PriceAmount">The product's initial price amount.</param>
/// <param name="PriceCurrency">The product's initial price currency.</param>
/// <param name="StockQuantity">The product's initial stock quantity.</param>
/// <param name="BrandId">The identifier of the product's brand.</param>
/// <param name="CategoryId">The identifier of the product's category.</param>
[DomainEventName("catalog.product-created.v1")]
public sealed record ProductCreatedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProductId,
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int StockQuantity,
    Guid BrandId,
    Guid CategoryId) : IDomainEvent;
