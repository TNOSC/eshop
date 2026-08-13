// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// Composes the Redis key a customer's basket document lives under, in one place so
/// <see cref="RedisBasketRepository"/> and <see cref="RedisBasketReader"/> cannot drift onto
/// different keys for the same customer.
/// </summary>
internal static class BasketKeys
{
    /// <summary>
    /// Composes the key for a customer's basket document.
    /// </summary>
    /// <param name="prefix">The configured key prefix.</param>
    /// <param name="customerId">The identifier of the customer the basket belongs to.</param>
    /// <returns>The Redis key, shaped <c>"{prefix}:{customerId}"</c>.</returns>
    public static string ForCustomer(string prefix, Guid customerId) =>
        string.Create(provider: CultureInfo.InvariantCulture, handler: $"{prefix}:{customerId}");
}
