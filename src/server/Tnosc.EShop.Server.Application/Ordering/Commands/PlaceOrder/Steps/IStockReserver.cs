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
/// The fourth step of <see cref="IPlaceOrderWorkflow"/>: establish that the catalogue can actually
/// supply what the order asks for.
/// </summary>
public interface IStockReserver
{
    /// <summary>
    /// Checks the order's lines against catalogue stock.
    /// </summary>
    /// <param name="order">The order to check.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// Success, or an <c>Order.InsufficientStock</c> conflict naming the first product that is short.
    /// </returns>
    ValueTask<Result> ReserveAsync(Order order, CancellationToken cancellationToken = default);
}
