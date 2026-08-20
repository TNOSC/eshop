// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// The second step of <see cref="IPlaceOrderWorkflow"/>: work out where the order is to be delivered.
/// </summary>
public interface ICustomerResolver
{
    /// <summary>
    /// Resolves the customer's default address into the snapshot the order will keep.
    /// </summary>
    /// <param name="customerId">The identifier of the customer placing the order.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The shipping address, an <c>Order.NoShippingAddress</c> conflict when the customer has no
    /// profile or no default address, or a <c>ShippingAddress.*</c> validation error.
    /// </returns>
    ValueTask<Result<ShippingAddress>> ResolveAsync(Guid customerId, CancellationToken cancellationToken = default);
}
