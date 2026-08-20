// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Application.Ordering.Commands.ConfirmOrder;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Infrastructure.External.Payment;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;

namespace Tnosc.EShop.Server.Tests.Integration.Payment;

/// <summary>
/// The full order → payment → order loop through the outbox, in both directions, against real
/// Postgres. This is what T4–T6's outbox and T2's decorator ordering were ultimately built to carry.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class PaymentOutboxTests(PostgresFixture fixture) : PaymentIntegrationTestBase(fixture)
{
    [Fact]
    public async Task PlacingAndConfirmingAnOrder_Should_EndWithItPaid_Once_TheAutomaticWalletPaymentSettlesThroughTheOutbox()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "PAY-OUT-1", name: "Widget", amount: 20.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 2);
        await DrainOutboxAsync();

        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);
        placed.IsSuccess.ShouldBeTrue();
        await ConfirmAsync(orderId: placed.Value.Value, customerId: customerId);

        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(
            expected: OrderStatus.Confirmed,
            customMessage: "nothing has touched payment yet");

        // Act — first pass: OrderPlaced is delivered, Payment's automatic reaction opens a wallet
        // payment and captures it immediately, writing PaymentSucceeded to the outbox in the same
        // transaction.
        await DrainOutboxAsync();

        Domain.Payment.Payments.Payment payment = (await PaymentRepository.GetByOrderIdAsync(orderId: placed.Value.Value))!;
        payment.Method.ShouldBe(expected: PaymentMethod.Wallet);
        payment.Status.ShouldBe(expected: PaymentStatus.Captured);

        // Act — second pass: PaymentSucceeded, written during the first pass, is now delivered.
        await DrainOutboxAsync();

        // Assert
        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(expected: OrderStatus.Paid);
    }

    [Fact]
    public async Task ADecliningCardPayment_Should_EndWithTheOrderCancelled_Once_PaymentFailedIsDelivered()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "PAY-OUT-2", name: "Widget", amount: 15.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 1);
        await DrainOutboxAsync();

        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);
        placed.IsSuccess.ShouldBeTrue();

        // Act — drive the payment explicitly with a declining card, before the automatic Wallet
        // reaction has a chance to run, so this order's one-payment-per-order slot is claimed by the
        // card attempt this test is about.
        Result<PaymentId> initiated = await InitiatePaymentAsync(
            orderId: placed.Value.Value,
            amount: 15.00m,
            method: PaymentMethod.Card,
            paymentReference: FakeCardNumbers.Declined);
        initiated.IsSuccess.ShouldBeTrue(customMessage: "a decline is a business outcome, not a command failure");

        Domain.Payment.Payments.Payment payment = (await PaymentRepository.GetByOrderIdAsync(orderId: placed.Value.Value))!;
        payment.Status.ShouldBe(expected: PaymentStatus.Failed);

        // Assert — before the outbox delivers PaymentFailed, the order is still exactly as placed.
        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(expected: OrderStatus.Pending);

        // Act
        await DrainOutboxAsync();

        // Assert
        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(expected: OrderStatus.Cancelled);
    }

    [Fact]
    public async Task RedeliveringPaymentSucceeded_Should_NotMarkTheOrderPaidTwice()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "PAY-OUT-3", name: "Widget", amount: 10.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 1);
        await DrainOutboxAsync();

        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);
        await ConfirmAsync(orderId: placed.Value.Value, customerId: customerId);
        await DrainOutboxAsync(); // delivers OrderPlaced -> opens + captures the wallet payment
        await DrainOutboxAsync(); // delivers PaymentSucceeded -> marks the order paid

        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(expected: OrderStatus.Paid);
        (await ClaimCountAsync(handlerNameContains: "PaymentSucceededMarkOrderPaid")).ShouldBe(expected: 1);

        // Act — exactly the state a crash between publishing PaymentSucceeded and marking it
        // processed would leave behind.
        await ResetRowToPendingAsync(contractName: "payment.payment-succeeded.v1");
        await DrainOutboxAsync();

        // Assert
        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(
            expected: OrderStatus.Paid,
            customMessage: "a redelivered PaymentSucceeded must not attempt MarkPaid a second time");
        (await ClaimCountAsync(handlerNameContains: "PaymentSucceededMarkOrderPaid")).ShouldBe(
            expected: 1,
            customMessage: "the redelivery must not add a second inbox claim");
    }

    [Fact]
    public async Task RedeliveringPaymentFailed_Should_NotCancelAnAlreadyCancelledOrderTwice()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "PAY-OUT-4", name: "Widget", amount: 10.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 1);

        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);
        await InitiatePaymentAsync(
            orderId: placed.Value.Value,
            amount: 10.00m,
            method: PaymentMethod.Card,
            paymentReference: FakeCardNumbers.Declined);
        await DrainOutboxAsync();

        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(expected: OrderStatus.Cancelled);
        (await ClaimCountAsync(handlerNameContains: "PaymentFailedCancelOrder")).ShouldBe(expected: 1);

        // Act
        await ResetRowToPendingAsync(contractName: "payment.payment-failed.v1");
        await DrainOutboxAsync();

        // Assert
        (await OrderStatusAsync(orderId: placed.Value.Value)).ShouldBe(expected: OrderStatus.Cancelled);
        (await ClaimCountAsync(handlerNameContains: "PaymentFailedCancelOrder")).ShouldBe(expected: 1);
    }

    private async Task DrainOutboxAsync() =>
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

    private async Task ConfirmAsync(Guid orderId, Guid customerId)
    {
        Result confirmed = await Scope.ServiceProvider
            .GetRequiredService<ICommandHandler<ConfirmOrderCommand>>()
            .HandleAsync(
                command: new ConfirmOrderCommand(OrderId: orderId, CustomerId: customerId),
                cancellationToken: CancellationToken.None);

        confirmed.IsSuccess.ShouldBeTrue();
    }

    private async Task<int> ClaimCountAsync(string handlerNameContains)
    {
        WriteContext.ChangeTracker.Clear();

        return await WriteContext.Set<ProcessedEvent>()
            .CountAsync(predicate: claim => claim.Handler.Contains(handlerNameContains));
    }

    private async Task ResetRowToPendingAsync(string contractName) =>
        await WriteContext.Database.ExecuteSqlRawAsync(
            sql: $"UPDATE {OutboxMessageConfiguration.SchemaName}.{OutboxMessageConfiguration.TableName} " +
                 "SET processed_on_utc = NULL, attempts = 0, next_attempt_on_utc = NULL " +
                 "WHERE type = {0}",
            parameters: contractName);
}
