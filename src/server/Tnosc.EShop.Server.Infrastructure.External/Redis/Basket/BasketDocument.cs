// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// The whole basket, serialized as the single JSON document stored under one Redis key. This is the
/// serialization boundary: the domain's <c>Basket</c>/<c>BasketItem</c> types never carry a
/// <c>[JsonPropertyName]</c> or a <c>[JsonConstructor]</c>, and <c>System.Text.Json</c> never has to
/// reach a private setter.
/// </summary>
/// <param name="BasketId">The basket's identifier.</param>
/// <param name="CustomerId">The identifier of the customer the basket belongs to.</param>
/// <param name="Items">The basket's lines.</param>
/// <param name="Version">
/// The basket's version at the time it was written — the field the optimistic-concurrency Lua script
/// in <see cref="RedisBasketRepository"/> compares against on the next write.
/// </param>
internal sealed record BasketDocument(
    Guid BasketId,
    Guid CustomerId,
    BasketItemDocument[] Items,
    int Version);
