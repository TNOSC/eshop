// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// The first step of <see cref="IPlaceOrderWorkflow"/>: fetch the customer's basket and establish
/// that there is something in it to order.
/// </summary>
public interface IBasketResolver
{
    /// <summary>
    /// Fetches the customer's basket.
    /// </summary>
    /// <param name="customerId">The identifier of the customer placing the order.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The basket, or an <c>Order.BasketEmpty</c> conflict when the customer has no basket or an empty
    /// one.
    /// </returns>
    ValueTask<Result<OrderBasketSnapshot>> ResolveAsync(Guid customerId, CancellationToken cancellationToken = default);
}
