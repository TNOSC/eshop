// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Ordering.Ports;

/// <summary>
/// The basket an order is being placed from, as Ordering sees it.
/// </summary>
/// <param name="CustomerId">The identifier of the customer the basket belongs to.</param>
/// <param name="Lines">The basket's lines. Never <see langword="null"/>, possibly empty.</param>
public sealed record OrderBasketSnapshot(Guid CustomerId, OrderBasketLine[] Lines);
