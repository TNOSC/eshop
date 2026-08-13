// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;

/// <summary>
/// Delegates to <see cref="IPlaceOrderWorkflow"/> and does nothing else.
/// </summary>
/// <remarks>
/// <para>
/// One line, deliberately. Everything placing an order involves lives in the workflow and its steps;
/// what stays here is what the decorator pipeline needs to see — the command type, the response type,
/// and the two attributes below. Adding a line to this method is the first sign a decision has leaked
/// out of the workflow.
/// </para>
/// <para>
/// <c>[Idempotent]</c> because this is a create over a network: a client whose connection drops
/// mid-request cannot tell whether an order exists, and without a key its only options are to give up
/// or risk charging the customer twice. With one, the retry replays the original
/// <see cref="OrderId"/>. The <c>Idempotency-Key</c> header is therefore part of this endpoint's
/// contract — a request without one is rejected, not run unguarded.
/// </para>
/// </remarks>
/// <param name="workflow">The workflow that composes the steps.</param>
[Idempotent]
internal sealed class PlaceOrderCommandHandler(IPlaceOrderWorkflow workflow)
    : ICommandHandler<PlaceOrderCommand, OrderId>
{
    /// <inheritdoc />
    public ValueTask<Result<OrderId>> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken = default) =>
        workflow.ExecuteAsync(command: command, cancellationToken: cancellationToken);
}
