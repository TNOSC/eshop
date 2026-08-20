// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Payment.Payments;

/// <summary>
/// Every failure a caller can get back about a <see cref="Payment"/>, defined once.
/// </summary>
/// <remarks>
/// The illegal-transition errors are all <c>Conflict</c>, matching <c>OrderErrors</c>' shape — an
/// attempt to capture an already-refunded payment surfaces as <strong>409</strong>, naming the status
/// the payment was actually in.
/// </remarks>
public static class PaymentErrors
{
    /// <summary>
    /// No payment carries the requested identifier.
    /// </summary>
    /// <param name="paymentId">The identifier that was looked up.</param>
    public static Error NotFound(Guid paymentId) => Error.NotFound(
        code: "Payment.NotFound",
        description: $"Payment {paymentId} was not found.");

    /// <summary>
    /// No payment has been initiated for the requested order.
    /// </summary>
    /// <param name="orderId">The order identifier that was looked up.</param>
    public static Error NotFoundForOrder(Guid orderId) => Error.NotFound(
        code: "Payment.NotFoundForOrder",
        description: $"No payment has been initiated for order {orderId}.");

    /// <summary>
    /// A payment has already been initiated for the requested order.
    /// </summary>
    /// <remarks>
    /// The uniqueness invariant a <see cref="PaymentFactory"/> enforces before <see cref="Payment.Create"/>
    /// is ever reached — the same shape as Catalog's SKU-uniqueness rule.
    /// </remarks>
    /// <param name="orderId">The order identifier a payment already exists for.</param>
    public static Error AlreadyExistsForOrder(Guid orderId) => Error.Conflict(
        code: "Payment.AlreadyExistsForOrder",
        description: $"A payment has already been initiated for order {orderId}.");

    /// <summary>
    /// Gets the error returned when an order identifier is missing.
    /// </summary>
    public static Error OrderRequired => Error.Validation(
        code: "Payment.OrderRequired",
        description: "An order identifier is required.");

    /// <summary>
    /// Gets the error returned when a payment amount is zero or negative.
    /// </summary>
    public static Error AmountMustBePositive => Error.Validation(
        code: "Payment.AmountMustBePositive",
        description: "A payment amount must be greater than zero.");

    /// <summary>
    /// Cash on delivery is not offered above the scheme's limit.
    /// </summary>
    /// <param name="limit">The largest amount cash on delivery covers.</param>
    public static Error CashOnDeliveryLimitExceeded(decimal limit) => Error.Conflict(
        code: "Payment.CashOnDeliveryLimitExceeded",
        description: $"Cash on delivery is not available for orders over {limit}.");

    /// <summary>
    /// The payment cannot be authorized from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the payment is actually in.</param>
    public static Error CannotAuthorize(PaymentStatus status) => Error.Conflict(
        code: "Payment.CannotAuthorize",
        description: Describe(action: "authorized", status: status));

    /// <summary>
    /// The payment cannot be captured from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the payment is actually in.</param>
    public static Error CannotCapture(PaymentStatus status) => Error.Conflict(
        code: "Payment.CannotCapture",
        description: Describe(action: "captured", status: status));

    /// <summary>
    /// The payment cannot be marked failed from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the payment is actually in.</param>
    public static Error CannotFail(PaymentStatus status) => Error.Conflict(
        code: "Payment.CannotFail",
        description: Describe(action: "marked failed", status: status));

    /// <summary>
    /// The payment cannot be refunded from the status it is currently in.
    /// </summary>
    /// <param name="status">The status the payment is actually in.</param>
    public static Error CannotRefund(PaymentStatus status) => Error.Conflict(
        code: "Payment.CannotRefund",
        description: Describe(action: "refunded", status: status));

    private static string Describe(string action, PaymentStatus status) =>
        $"A payment cannot be {action} while it is {status}.";
}
