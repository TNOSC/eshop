// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// How much of a discount a customer's history entitles them to.
/// </summary>
/// <remarks>
/// Owned by Ordering, not Identity: a tier is a fact about how much someone has ordered, which is
/// Ordering's own data. Identity holds the profile — name, phone, addresses — and knows nothing about
/// loyalty. The thresholds that map an order count onto one of these values live in
/// <see cref="CustomerTierFactory"/>.
/// </remarks>
public enum CustomerTier
{
    /// <summary>
    /// A customer with no meaningful order history yet.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// A returning customer.
    /// </summary>
    Silver = 1,

    /// <summary>
    /// A long-standing, high-volume customer.
    /// </summary>
    Gold = 2,
}
