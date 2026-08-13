// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// Builds <see cref="PaymentAggregate"/> instances for tests through the real factory and the real
/// transitions — never by reaching into private state. Goes through <see cref="PaymentFactory"/> with
/// a repository that reports no order clash, because <c>Payment.Create</c> is deliberately internal
/// to the domain — the factory is the only way in, for tests as much as for handlers.
/// </summary>
internal static class PaymentTestFactory
{
    /// <summary>
    /// The default amount every helper here charges unless a test supplies its own.
    /// </summary>
    public const decimal DefaultAmount = 50.00m;

    /// <summary>
    /// The currency every helper here charges in unless a test supplies its own.
    /// </summary>
    public const string DefaultCurrency = "EUR";

    /// <summary>
    /// Creates a pending payment.
    /// </summary>
    /// <param name="orderId">The order the payment is for, defaulted to a fresh identifier.</param>
    /// <param name="method">How the customer is paying, defaulted to <see cref="PaymentMethod.Card"/>.</param>
    /// <param name="amount">The amount to collect, defaulted to <see cref="DefaultAmount"/>.</param>
    /// <returns>The created, pending payment.</returns>
    public static async Task<PaymentAggregate> PendingAsync(
        Guid? orderId = null,
        PaymentMethod method = PaymentMethod.Card,
        decimal amount = DefaultAmount)
    {
        IPaymentRepository repository = Substitute.For<IPaymentRepository>();
        repository
            .GetByOrderIdAsync(orderId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: null));

        Result<PaymentAggregate> payment = await PaymentFactory.CreateAsync(
            repository: repository,
            orderId: orderId ?? Guid.CreateVersion7(),
            amount: Money.Create(amount: amount, currency: DefaultCurrency).Value,
            method: method);

        return payment.Value;
    }

    /// <summary>
    /// Creates a payment already moved to <see cref="PaymentStatus.Authorized"/>.
    /// </summary>
    /// <param name="orderId">The order the payment is for, defaulted to a fresh identifier.</param>
    /// <returns>The authorized payment.</returns>
    public static async Task<PaymentAggregate> AuthorizedAsync(Guid? orderId = null)
    {
        PaymentAggregate payment = await PendingAsync(orderId: orderId);
        payment.Authorize(gatewayReference: "auth_test");

        return payment;
    }

    /// <summary>
    /// Creates a payment already moved to <see cref="PaymentStatus.Captured"/>.
    /// </summary>
    /// <param name="orderId">The order the payment is for, defaulted to a fresh identifier.</param>
    /// <returns>The captured payment.</returns>
    public static async Task<PaymentAggregate> CapturedAsync(Guid? orderId = null)
    {
        PaymentAggregate payment = await AuthorizedAsync(orderId: orderId);
        payment.Capture(gatewayReference: "cap_test");

        return payment;
    }

    /// <summary>
    /// Creates a payment already moved to <see cref="PaymentStatus.Failed"/>.
    /// </summary>
    /// <param name="orderId">The order the payment is for, defaulted to a fresh identifier.</param>
    /// <returns>The failed payment.</returns>
    public static async Task<PaymentAggregate> FailedAsync(Guid? orderId = null)
    {
        PaymentAggregate payment = await PendingAsync(orderId: orderId);
        payment.Fail(reason: "card_declined");

        return payment;
    }

    /// <summary>
    /// Creates a payment already moved to <see cref="PaymentStatus.Refunded"/>.
    /// </summary>
    /// <param name="orderId">The order the payment is for, defaulted to a fresh identifier.</param>
    /// <returns>The refunded payment.</returns>
    public static async Task<PaymentAggregate> RefundedAsync(Guid? orderId = null)
    {
        PaymentAggregate payment = await CapturedAsync(orderId: orderId);
        payment.Refund(reason: "customer request");

        return payment;
    }
}
