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
/// Reads the caller's own basket. Cached for a minute, keyed by <see cref="CustomerId"/>, and
/// invalidated by every Basket write handler under the <c>basket</c> cache tag.
/// </summary>
/// <param name="CustomerId">The identifier of the customer whose basket to read.</param>
public sealed record GetBasketQuery([property: CacheKey] Guid CustomerId) : IQuery<BasketDto>;
