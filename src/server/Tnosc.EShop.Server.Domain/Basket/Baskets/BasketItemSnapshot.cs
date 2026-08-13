// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Shared;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// The stored shape of a single <see cref="BasketItem"/>, used only to hand data into
/// <see cref="Basket.Rehydrate"/> from outside the domain assembly.
/// </summary>
/// <remarks>
/// <see cref="BasketItem"/>'s own construction methods are <see langword="internal"/> — only
/// <see cref="Basket"/> may build its lines. A caller reconstructing a basket from a stored document
/// (Redis, in <c>Server.Infrastructure.External</c>) therefore cannot call them directly; it builds
/// this plain record from already-validated value objects instead, and <see cref="Basket.Rehydrate"/>
/// does the actual construction from inside the domain assembly.
/// </remarks>
/// <param name="ItemId">The line's stored identifier.</param>
/// <param name="ProductId">The stored product identifier.</param>
/// <param name="Sku">The stored SKU snapshot.</param>
/// <param name="ProductName">The stored product name snapshot.</param>
/// <param name="UnitPrice">The stored unit price snapshot.</param>
/// <param name="Quantity">The stored quantity.</param>
public sealed record BasketItemSnapshot(
    BasketItemId ItemId,
    Guid ProductId,
    string Sku,
    string ProductName,
    Money UnitPrice,
    Quantity Quantity);
