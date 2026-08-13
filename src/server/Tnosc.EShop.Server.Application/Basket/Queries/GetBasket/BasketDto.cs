// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;

/// <summary>
/// A customer's basket, projected for reading.
/// </summary>
/// <param name="BasketId">The basket's identifier.</param>
/// <param name="CustomerId">The identifier of the customer the basket belongs to.</param>
/// <param name="Items">The basket's lines.</param>
/// <param name="TotalAmount">The basket's total amount, or <see langword="null"/> when it has no lines.</param>
/// <param name="TotalCurrency">The basket's total currency, or <see langword="null"/> when it has no lines.</param>
public sealed record BasketDto(
    Guid BasketId,
    Guid CustomerId,
    BasketItemDto[] Items,
    decimal? TotalAmount,
    string? TotalCurrency);
