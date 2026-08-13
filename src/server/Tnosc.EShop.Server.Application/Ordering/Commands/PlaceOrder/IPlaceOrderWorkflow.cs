// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;

/// <summary>
/// Runs the several steps placing an order takes.
/// </summary>
/// <remarks>
/// Extracted from the handler rather than injected into it. Placing an order needs the basket, the
/// customer's address, the discount rules, the catalogue's stock levels and the order repository —
/// five collaborators, which in a single handler is the god-handler shape the design doc calls out by
/// name. The handler keeps one dependency, this interface; the composition lives in
/// <see cref="PlaceOrderWorkflow"/>; and each step is separately testable.
/// </remarks>
public interface IPlaceOrderWorkflow
{
    /// <summary>
    /// Places an order for the customer named by the command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The new order's identifier, or the first failing step's error — the workflow short-circuits, so
    /// no later step runs once one has failed.
    /// </returns>
    ValueTask<Result<OrderId>> ExecuteAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default);
}
