// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain.Repositories;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Command-side persistence contract for <see cref="Order"/>.
/// </summary>
public interface IOrderRepository : IRepository<Order, OrderId>
{
    /// <summary>
    /// Retrieves every order a customer has placed.
    /// </summary>
    /// <param name="customerId">The identifier of the customer whose orders to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The customer's orders, newest first. Empty when they have placed none.</returns>
    ValueTask<IReadOnlyCollection<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the order carrying a human-facing order number.
    /// </summary>
    /// <param name="orderNumber">The order number to look up.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching order, or <see langword="null"/> when none carries that number.</returns>
    ValueTask<Order?> GetByOrderNumberAsync(OrderNumber orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves one of a specific customer's orders.
    /// </summary>
    /// <remarks>
    /// The customer identifier is part of the lookup rather than something a handler checks afterwards.
    /// A customer asking for an order that is not theirs gets <see langword="null"/> — indistinguishable
    /// from one that does not exist — so ownership is structural and no handler ever contains an
    /// <c>if (order.CustomerId != callerId)</c>. See <c>.claude/rules/authorization.md</c>.
    /// </remarks>
    /// <param name="orderId">The identifier of the order to retrieve.</param>
    /// <param name="customerId">The identifier of the customer the order must belong to.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching order, or <see langword="null"/> when it does not exist or is not theirs.</returns>
    ValueTask<Order?> GetByIdForCustomerAsync(
        OrderId orderId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the orders a customer has already placed.
    /// </summary>
    /// <remarks>
    /// Feeds <see cref="CustomerTierFactory.For"/>, which turns the count into the tier
    /// <see cref="Discounts.DiscountStrategyFactory"/> prices against.
    /// </remarks>
    /// <param name="customerId">The identifier of the customer to count orders for.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>How many orders the customer has placed.</returns>
    ValueTask<int> CountByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
