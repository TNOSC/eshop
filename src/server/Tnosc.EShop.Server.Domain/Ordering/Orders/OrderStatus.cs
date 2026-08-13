// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Where an <see cref="Order"/> sits in its lifecycle.
/// </summary>
/// <remarks>
/// Only <see cref="Order"/> ever compares one of these values. A handler that reads
/// <see cref="Order.Status"/> to decide anything has taken a business decision that belongs to the
/// aggregate, and <c>NoBusinessBranchingTests</c> fails the build for it — which is the point: the
/// legal transitions are <see cref="Order.Confirm"/>, <see cref="Order.MarkPaid"/>,
/// <see cref="Order.Ship"/>, <see cref="Order.Deliver"/> and <see cref="Order.Cancel"/>, each of
/// which decides for itself whether it is reachable from here.
/// </remarks>
public enum OrderStatus
{
    /// <summary>
    /// The order has been placed but not yet confirmed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The order has been confirmed and is awaiting payment.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// The order has been paid for and is awaiting despatch.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// The order has left the warehouse.
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// The order has reached the customer.
    /// </summary>
    Delivered = 4,

    /// <summary>
    /// The order was cancelled before despatch.
    /// </summary>
    Cancelled = 5,
}
