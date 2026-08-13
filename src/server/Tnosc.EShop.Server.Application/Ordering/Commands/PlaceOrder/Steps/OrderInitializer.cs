// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// Maps the basket onto <see cref="OrderLineDraft"/>s, asks the domain which discount applies, and
/// hands both to <see cref="Order.Create"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every decision here is delegated, none taken. The tier comes from
/// <see cref="CustomerTierFactory.For"/>, the strategy from
/// <see cref="DiscountStrategyFactory.Create"/>, and the line validation from
/// <see cref="Order.Create"/>. What is left in this class is copying fields and one repository call —
/// which is exactly how much orchestration a step is supposed to contain.
/// </para>
/// <para>
/// The provisional total handed to the factory is built from the basket's own prices rather than
/// recomputed, because the tier threshold is a rule about what the customer is spending, and the
/// authoritative subtotal is <see cref="Order.Subtotal"/> once the lines exist. The strategy is
/// applied to <em>that</em> inside <see cref="Order.Create"/>; the value passed here only selects the
/// scheme.
/// </para>
/// </remarks>
/// <param name="orderRepository">The order repository, consulted for the customer's order count.</param>
internal sealed class OrderInitializer(IOrderRepository orderRepository) : IOrderInitializer
{
    /// <inheritdoc />
    public async ValueTask<Result<Order>> InitializeAsync(
        Guid customerId,
        OrderBasketSnapshot basket,
        ShippingAddress shippingAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: basket);

        List<OrderLineDraft> drafts = [.. basket.Lines.Select(selector: static line => new OrderLineDraft(
            ProductId: line.ProductId,
            Sku: line.Sku,
            ProductName: line.ProductName,
            UnitPriceAmount: line.UnitPriceAmount,
            UnitPriceCurrency: line.UnitPriceCurrency,
            Quantity: line.Quantity))];

        Result<Money> provisionalTotal = Money.Create(
            amount: basket.Lines.Sum(selector: static line => line.UnitPriceAmount * line.Quantity),
            currency: basket.Lines[0].UnitPriceCurrency);

        if (provisionalTotal.IsError)
        {
            return provisionalTotal.Errors.ToArray();
        }

        int previousOrderCount = await orderRepository.CountByCustomerIdAsync(
            customerId: customerId,
            cancellationToken: cancellationToken);

        IDiscountStrategy discountStrategy = DiscountStrategyFactory.Create(
            total: provisionalTotal.Value,
            tier: CustomerTierFactory.For(previousOrderCount: previousOrderCount));

        return Order.Create(
            customerId: customerId,
            shippingAddress: shippingAddress,
            lines: drafts,
            discountStrategy: discountStrategy);
    }
}
