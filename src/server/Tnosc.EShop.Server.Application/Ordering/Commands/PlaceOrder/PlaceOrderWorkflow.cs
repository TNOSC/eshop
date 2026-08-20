// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;

/// <summary>
/// Runs the five steps of placing an order in order, stopping at the first that fails.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This class composes; it decides nothing.</strong> Every guard below tests one thing —
/// whether the step before it succeeded — and every one of them returns that step's errors untouched.
/// The order of the steps is the only judgement encoded here, and it is not arbitrary: the basket
/// establishes there is an order to place at all, the address and the discount are needed to build the
/// aggregate, the stock check needs the built lines, and persistence goes last because it is the only
/// step that writes.
/// </para>
/// <para>
/// Short-circuiting is <em>error propagation</em>, which the architecture rules permit explicitly.
/// Written as sequential early returns rather than nested conditionals or a chaining combinator: at
/// five steps the guards read top to bottom in the order they run, and a combinator would buy
/// indirection rather than clarity. The property that matters — <strong>no step runs after one has
/// failed</strong> — is what <c>PlaceOrderWorkflowTests</c> asserts directly.
/// </para>
/// </remarks>
/// <param name="basketResolver">Step 1 — fetch the basket and check it is not empty.</param>
/// <param name="customerResolver">Step 2 — resolve the delivery address.</param>
/// <param name="orderInitializer">Step 3 — build the order under the discount it qualifies for.</param>
/// <param name="stockReserver">Step 4 — check the catalogue can supply the lines.</param>
/// <param name="orderPersister">Step 5 — commit, writing the outbox row in the same transaction.</param>
internal sealed class PlaceOrderWorkflow(
    IBasketResolver basketResolver,
    ICustomerResolver customerResolver,
    IOrderInitializer orderInitializer,
    IStockReserver stockReserver,
    IOrderPersister orderPersister)
    : IPlaceOrderWorkflow
{
    /// <inheritdoc />
    public async ValueTask<Result<OrderId>> ExecuteAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<OrderBasketSnapshot> basket = await basketResolver.ResolveAsync(
            customerId: command.CustomerId,
            cancellationToken: cancellationToken);

        if (basket.IsError)
        {
            return basket.Errors.ToArray();
        }

        Result<ShippingAddress> shippingAddress = await customerResolver.ResolveAsync(
            customerId: command.CustomerId,
            cancellationToken: cancellationToken);

        if (shippingAddress.IsError)
        {
            return shippingAddress.Errors.ToArray();
        }

        Result<Order> order = await orderInitializer.InitializeAsync(
            customerId: command.CustomerId,
            basket: basket.Value,
            shippingAddress: shippingAddress.Value,
            cancellationToken: cancellationToken);

        if (order.IsError)
        {
            return order.Errors.ToArray();
        }

        Result reserved = await stockReserver.ReserveAsync(
            order: order.Value,
            cancellationToken: cancellationToken);

        if (reserved.IsError)
        {
            return reserved.Errors.ToArray();
        }

        return await orderPersister.PersistAsync(
            order: order.Value,
            cancellationToken: cancellationToken);
    }
}
