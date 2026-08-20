// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// The third step of <see cref="IPlaceOrderWorkflow"/>: build the <see cref="Order"/> aggregate from
/// the basket, the address and the discount the customer qualifies for.
/// </summary>
public interface IOrderInitializer
{
    /// <summary>
    /// Builds the order.
    /// </summary>
    /// <param name="customerId">The identifier of the customer placing the order.</param>
    /// <param name="basket">The basket the order is being placed from.</param>
    /// <param name="shippingAddress">Where the order is to be delivered.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The unsaved order, or whatever validation error <see cref="Order.Create"/> produced.</returns>
    ValueTask<Result<Order>> InitializeAsync(
        Guid customerId,
        OrderBasketSnapshot basket,
        ShippingAddress shippingAddress,
        CancellationToken cancellationToken = default);
}
