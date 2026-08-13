// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;

/// <summary>
/// Reads the caller's own basket.
/// </summary>
/// <param name="CustomerId">The identifier of the customer whose basket to read.</param>
public sealed record GetBasketQuery(Guid CustomerId) : IQuery<BasketDto>;
