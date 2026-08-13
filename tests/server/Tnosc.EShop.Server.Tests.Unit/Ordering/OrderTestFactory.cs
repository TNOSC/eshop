// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// Builds <see cref="Order"/> instances for tests through the real factory and the real transitions —
/// never by reaching into private state.
/// </summary>
/// <remarks>
/// The status helpers walk the aggregate forward the way production does, so a test that says "given a
/// paid order" is exercising the same path that produced one. If a transition ever stops being
/// reachable, these helpers fail loudly rather than fabricating a state the domain would refuse.
/// </remarks>
internal static class OrderTestFactory
{
    /// <summary>
    /// The default line every helper here orders unless a test supplies its own.
    /// </summary>
    public const decimal DefaultUnitPrice = 25.00m;

    /// <summary>
    /// The currency every helper here prices in unless a test supplies its own.
    /// </summary>
    public const string DefaultCurrency = "EUR";

    /// <summary>
    /// Creates a pending order.
    /// </summary>
    /// <param name="customerId">The placing customer, defaulted to a fresh identifier.</param>
    /// <param name="lines">The lines to order, defaulted to a single line of one unit.</param>
    /// <param name="discountStrategy">The discount to apply, defaulted to none.</param>
    /// <returns>The created order.</returns>
    public static Order Pending(
        Guid? customerId = null,
        IReadOnlyCollection<OrderLineDraft>? lines = null,
        IDiscountStrategy? discountStrategy = null) =>
        Order.Create(
            customerId: customerId ?? Guid.CreateVersion7(),
            shippingAddress: Address(),
            lines: lines ?? [Line()],
            discountStrategy: discountStrategy ?? new NoDiscountStrategy()).Value;

    /// <summary>
    /// Creates an order already moved to <see cref="OrderStatus.Confirmed"/>.
    /// </summary>
    /// <param name="customerId">The placing customer, defaulted to a fresh identifier.</param>
    /// <returns>The confirmed order.</returns>
    public static Order Confirmed(Guid? customerId = null)
    {
        Order order = Pending(customerId: customerId);
        order.Confirm();

        return order;
    }

    /// <summary>
    /// Creates an order already moved to <see cref="OrderStatus.Paid"/>.
    /// </summary>
    /// <param name="customerId">The placing customer, defaulted to a fresh identifier.</param>
    /// <returns>The paid order.</returns>
    public static Order Paid(Guid? customerId = null)
    {
        Order order = Confirmed(customerId: customerId);
        order.MarkPaid();

        return order;
    }

    /// <summary>
    /// Creates an order already moved to <see cref="OrderStatus.Shipped"/>.
    /// </summary>
    /// <param name="customerId">The placing customer, defaulted to a fresh identifier.</param>
    /// <returns>The shipped order.</returns>
    public static Order Shipped(Guid? customerId = null)
    {
        Order order = Paid(customerId: customerId);
        order.Ship();

        return order;
    }

    /// <summary>
    /// Creates an order already moved to <see cref="OrderStatus.Delivered"/>.
    /// </summary>
    /// <param name="customerId">The placing customer, defaulted to a fresh identifier.</param>
    /// <returns>The delivered order.</returns>
    public static Order Delivered(Guid? customerId = null)
    {
        Order order = Shipped(customerId: customerId);
        order.Deliver();

        return order;
    }

    /// <summary>
    /// Creates an order already moved to <see cref="OrderStatus.Cancelled"/>.
    /// </summary>
    /// <param name="customerId">The placing customer, defaulted to a fresh identifier.</param>
    /// <returns>The cancelled order.</returns>
    public static Order Cancelled(Guid? customerId = null)
    {
        Order order = Pending(customerId: customerId);
        order.Cancel();

        return order;
    }

    /// <summary>
    /// Builds a valid line draft.
    /// </summary>
    /// <param name="productId">The product ordered, defaulted to a fresh identifier.</param>
    /// <param name="unitPrice">The unit price, defaulted to <see cref="DefaultUnitPrice"/>.</param>
    /// <param name="quantity">The number of units, defaulted to one.</param>
    /// <param name="currency">The currency, defaulted to <see cref="DefaultCurrency"/>.</param>
    /// <returns>The line draft.</returns>
    public static OrderLineDraft Line(
        Guid? productId = null,
        decimal unitPrice = DefaultUnitPrice,
        int quantity = 1,
        string currency = DefaultCurrency) =>
        new(ProductId: productId ?? Guid.CreateVersion7(),
            Sku: "WIDGET-1",
            ProductName: "Widget",
            UnitPriceAmount: unitPrice,
            UnitPriceCurrency: currency,
            Quantity: quantity);

    /// <summary>
    /// Builds a valid shipping address.
    /// </summary>
    /// <returns>The shipping address.</returns>
    public static ShippingAddress Address() =>
        ShippingAddress.Create(
            street: "1 Rue de Carthage",
            city: "Tunis",
            postalCode: "1000",
            country: "TN").Value;
}
