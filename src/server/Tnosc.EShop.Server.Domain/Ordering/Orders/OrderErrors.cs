// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Every failure a caller can get back about an <see cref="Order"/>, defined once.
/// </summary>
/// <remarks>
/// The illegal-transition errors are all <c>Conflict</c>, so an attempt to ship an unconfirmed order
/// surfaces as <strong>409</strong> rather than a 400 or a 500. They name the status the order was
/// actually in, because "you cannot ship this" without saying why is the kind of message that costs a
/// support call.
/// </remarks>
public static class OrderErrors
{
    /// <summary>
    /// No order carries the requested identifier — or it does, but belongs to another customer.
    /// </summary>
    /// <remarks>
    /// One error for both cases on purpose. A customer asking for someone else's order gets the same
    /// answer as one asking for an order that does not exist, so the endpoint cannot be used to probe
    /// which identifiers are real.
    /// </remarks>
    /// <param name="orderId">The identifier that was looked up.</param>
    public static Error NotFound(Guid orderId) => Error.NotFound(
        code: "Order.NotFound",
        description: $"Order {orderId} was not found.");

    /// <summary>
    /// No order carries the requested order number.
    /// </summary>
    /// <param name="orderNumber">The order number that was looked up.</param>
    public static Error NumberNotFound(string orderNumber) => Error.NotFound(
        code: "Order.NumberNotFound",
        description: $"Order '{orderNumber}' was not found.");

    /// <summary>
    /// Gets the error returned when an order is placed with no lines.
    /// </summary>
    public static Error NoLines => Error.Validation(
        code: "Order.NoLines",
        description: "An order must contain at least one line.");

    /// <summary>
    /// Gets the error returned when a customer identifier is missing.
    /// </summary>
    public static Error CustomerRequired => Error.Validation(
        code: "Order.CustomerRequired",
        description: "A customer identifier is required.");

    /// <summary>
    /// Gets the error returned when an order line names no product.
    /// </summary>
    public static Error ProductRequired => Error.Validation(
        code: "Order.ProductRequired",
        description: "Every order line must name a product.");

    /// <summary>
    /// Gets the error returned when an order line's SKU snapshot is missing or too long.
    /// </summary>
    public static Error InvalidSku => Error.Validation(
        code: "Order.InvalidSku",
        description: $"An order line's SKU is required and must be at most {OrderLine.SkuMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when an order line's product-name snapshot is missing or too long.
    /// </summary>
    public static Error InvalidProductName => Error.Validation(
        code: "Order.InvalidProductName",
        description: $"An order line's product name is required and must be at most {OrderLine.ProductNameMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when an order's lines are not all priced in the same currency.
    /// </summary>
    public static Error MixedCurrencies => Error.Validation(
        code: "Order.MixedCurrencies",
        description: "Every line of an order must be priced in the same currency.");

    /// <summary>
    /// The order cannot be confirmed from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the order is actually in.</param>
    public static Error CannotConfirm(OrderStatus status) => Error.Conflict(
        code: "Order.CannotConfirm",
        description: Describe(action: "confirmed", status: status));

    /// <summary>
    /// The order cannot be marked paid from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the order is actually in.</param>
    public static Error CannotMarkPaid(OrderStatus status) => Error.Conflict(
        code: "Order.CannotMarkPaid",
        description: Describe(action: "marked paid", status: status));

    /// <summary>
    /// The order cannot be shipped from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the order is actually in.</param>
    public static Error CannotShip(OrderStatus status) => Error.Conflict(
        code: "Order.CannotShip",
        description: Describe(action: "shipped", status: status));

    /// <summary>
    /// The order cannot be delivered from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the order is actually in.</param>
    public static Error CannotDeliver(OrderStatus status) => Error.Conflict(
        code: "Order.CannotDeliver",
        description: Describe(action: "delivered", status: status));

    /// <summary>
    /// The order cannot be cancelled from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the order is actually in.</param>
    public static Error CannotCancel(OrderStatus status) => Error.Conflict(
        code: "Order.CannotCancel",
        description: Describe(action: "cancelled", status: status));

    /// <summary>
    /// The order cannot be repriced from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the order is actually in.</param>
    public static Error CannotApplyDiscount(OrderStatus status) => Error.Conflict(
        code: "Order.CannotApplyDiscount",
        description: Describe(action: "repriced", status: status));

    /// <summary>
    /// The catalogue does not hold enough stock of a product to satisfy the order.
    /// </summary>
    /// <param name="productId">The identifier of the product that is short.</param>
    /// <param name="requested">The number of units the order asks for.</param>
    /// <param name="available">The number of units on hand.</param>
    public static Error InsufficientStock(
        Guid productId,
        int requested,
        int available) => Error.Conflict(
        code: "Order.InsufficientStock",
        description: string.Create(
            provider: CultureInfo.InvariantCulture,
            handler: $"Product {productId} has {available} units on hand, which does not cover the {requested} requested."));

    /// <summary>
    /// The customer's basket is empty, so there is nothing to place an order for.
    /// </summary>
    /// <param name="customerId">The identifier of the customer whose basket is empty.</param>
    public static Error BasketEmpty(Guid customerId) => Error.Conflict(
        code: "Order.BasketEmpty",
        description: $"Customer {customerId} has no basket lines to place an order for.");

    /// <summary>
    /// The customer has no profile, or no address to ship to.
    /// </summary>
    /// <param name="customerId">The identifier of the customer.</param>
    public static Error NoShippingAddress(Guid customerId) => Error.Conflict(
        code: "Order.NoShippingAddress",
        description: $"Customer {customerId} has no default shipping address to deliver to.");

    private static string Describe(string action, OrderStatus status) =>
        $"An order cannot be {action} while it is {status}.";
}
