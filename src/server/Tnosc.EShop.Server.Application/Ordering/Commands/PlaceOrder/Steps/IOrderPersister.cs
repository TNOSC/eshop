// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// The last step of <see cref="IPlaceOrderWorkflow"/>: commit the order.
/// </summary>
public interface IOrderPersister
{
    /// <summary>
    /// Adds the order and commits.
    /// </summary>
    /// <param name="order">The order to persist.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The persisted order's identifier.</returns>
    ValueTask<Result<OrderId>> PersistAsync(Order order, CancellationToken cancellationToken = default);
}
