// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// Bounds how baskets are keyed and how long they live in Redis, bound from the <c>"Basket"</c>
/// configuration section.
/// </summary>
public sealed class BasketOptions
{
    /// <summary>
    /// The configuration section this class binds to.
    /// </summary>
    public const string SectionName = "Basket";

    /// <summary>
    /// Gets or sets the prefix every basket key is composed under — <c>"{KeyPrefix}:{customerId}"</c>,
    /// see <see cref="BasketKeys"/>. Defaults to <c>"basket"</c>.
    /// </summary>
    [Required]
    public string KeyPrefix { get; set; } = "basket";

    /// <summary>
    /// Gets or sets the sliding time-to-live refreshed on every write. Defaults to 14 days — long
    /// enough that a returning customer's basket survives, without needing a cleanup job the way a
    /// Postgres table would.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "365.00:00:00")]
    public TimeSpan Ttl { get; set; } = TimeSpan.FromDays(value: 14);
}
