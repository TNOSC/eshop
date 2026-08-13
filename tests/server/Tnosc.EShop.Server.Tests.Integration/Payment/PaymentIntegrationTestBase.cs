// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment;
using Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.EShop.Server.Tests.Integration.Ordering;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Observabilities;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Tests.Integration.Payment;

/// <summary>
/// Seeding and resolution helpers shared by the Payment integration tests.
/// </summary>
/// <remarks>
/// Derives from <see cref="OrderingIntegrationTestBase"/> rather than <see cref="IntegrationTestBase"/>
/// directly, so a test can place a real order through the real Catalog/Identity/Basket/Ordering
/// pipelines and then drive its payment from there — exactly the order → payment → order loop this
/// slice exists to prove.
/// </remarks>
/// <param name="fixture">The shared Postgres/Redis fixture.</param>
public abstract class PaymentIntegrationTestBase(PostgresFixture fixture) : OrderingIntegrationTestBase(fixture)
{
    /// <summary>
    /// Gets the scoped payment repository, sharing this test's write context.
    /// </summary>
    protected IPaymentRepository PaymentRepository =>
        Scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

    /// <summary>
    /// Initiates a payment through the real, fully decorated command pipeline.
    /// </summary>
    /// <param name="orderId">The order to pay for.</param>
    /// <param name="amount">The amount to collect.</param>
    /// <param name="method">How the customer is paying.</param>
    /// <param name="paymentReference">The funding-source reference — a test card number for Card.</param>
    /// <param name="currency">The currency to collect in, defaulted to EUR.</param>
    /// <returns>The pipeline's result.</returns>
    protected ValueTask<Result<PaymentId>> InitiatePaymentAsync(
        Guid orderId,
        decimal amount,
        PaymentMethod method,
        string? paymentReference,
        string currency = "EUR")
    {
        // InitiatePaymentCommandHandler is not [Idempotent] (see its own remarks), so no ambient key
        // is required — unlike PlaceOrderAsync's sibling in OrderingIntegrationTestBase.
        IdempotencyKeyContext.Current = null;

        return Scope.ServiceProvider.GetRequiredService<ICommandHandler<InitiatePaymentCommand, PaymentId>>()
            .HandleAsync(
                command: new InitiatePaymentCommand(
                    OrderId: orderId,
                    AmountAmount: amount,
                    AmountCurrency: currency,
                    Method: method.ToString(),
                    PaymentReference: paymentReference),
                cancellationToken: CancellationToken.None);
    }

    /// <summary>
    /// Captures a payment through the real, fully decorated command pipeline.
    /// </summary>
    /// <param name="paymentId">The payment to capture.</param>
    /// <returns>The pipeline's result.</returns>
    protected ValueTask<Result> CapturePaymentAsync(Guid paymentId) =>
        Scope.ServiceProvider.GetRequiredService<ICommandHandler<CapturePaymentCommand>>()
            .HandleAsync(
                command: new CapturePaymentCommand(PaymentId: paymentId),
                cancellationToken: CancellationToken.None);

    /// <summary>
    /// Reads an order's current status directly from the write repository, bypassing the read side —
    /// used to observe an outbox-driven transition without waiting on read-model replication (both
    /// sides read the same table here, but this keeps the assertion honest about what changed).
    /// </summary>
    /// <param name="orderId">The order to read.</param>
    /// <returns>The order's current status.</returns>
    protected async Task<OrderStatus> OrderStatusAsync(Guid orderId)
    {
        WriteContext.ChangeTracker.Clear();

        Order order = await OrderRepository.GetByIdAsync(id: OrderId.From(value: orderId))
            ?? throw new InvalidOperationException(message: $"Order {orderId} was not found.");

        return order.Status;
    }
}
