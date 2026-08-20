// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderSummary;
using Tnosc.EShop.Server.Application.Payment.Queries.GetPaymentByOrder;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Infrastructure.External.Payment;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Tests.Integration.Payment;

/// <summary>
/// <c>GetPaymentByOrderQueryHandler</c> against real Postgres.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class PaymentQueryHandlerTests(PostgresFixture fixture) : PaymentIntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetPaymentByOrder_Should_ProjectTheInitiatedPayment()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Domain.Catalog.Products.Product product = await SeedProductAsync(sku: "PAY-Q-1", name: "Widget", amount: 30.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 2);
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        Result<PaymentId> initiated = await InitiatePaymentAsync(
            orderId: placed.Value.Value,
            amount: 60.00m,
            method: PaymentMethod.Card,
            paymentReference: FakeCardNumbers.Approved);

        // Act
        Result<PaymentDto> payment = await GetByOrderAsync(orderId: placed.Value.Value);

        // Assert
        payment.IsSuccess.ShouldBeTrue();
        payment.Value.Id.ShouldBe(expected: initiated.Value.Value);
        payment.Value.OrderId.ShouldBe(expected: placed.Value.Value);
        payment.Value.AmountAmount.ShouldBe(expected: 60.00m);
        payment.Value.AmountCurrency.ShouldBe(expected: "EUR");
        payment.Value.Method.ShouldBe(expected: nameof(PaymentMethod.Card));
        payment.Value.Status.ShouldBe(expected: nameof(PaymentStatus.Authorized), customMessage: "a card authorizes but does not capture during InitiatePayment");
        payment.Value.GatewayReference.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetPaymentByOrder_Should_ReturnNotFound_When_NoPaymentHasBeenInitiated()
    {
        // Act
        Result<PaymentDto> payment = await GetByOrderAsync(orderId: Guid.CreateVersion7());

        // Assert
        payment.IsError.ShouldBeTrue();
        payment.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        payment.FirstError.Code.ShouldBe(expected: "Payment.NotFoundForOrder");
    }

    [Fact]
    public async Task GetOrderSummary_Should_CarryThePaymentsStatusAndMethod_Once_OneHasBeenInitiated()
    {
        // Arrange — proves the T14 LEFT JOIN payment.payments addition to GetOrderSummaryQueryHandler.
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Domain.Catalog.Products.Product product = await SeedProductAsync(sku: "PAY-Q-2", name: "Widget", amount: 40.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 1);
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        await InitiatePaymentAsync(
            orderId: placed.Value.Value,
            amount: 40.00m,
            method: PaymentMethod.Wallet,
            paymentReference: "wallet-test");

        // Act
        Result<OrderSummaryReportDto> summary = await QueryHandler<GetOrderSummaryQuery, OrderSummaryReportDto>()
            .HandleAsync(query: new GetOrderSummaryQuery(OrderId: placed.Value.Value), cancellationToken: CancellationToken.None);

        // Assert
        summary.IsSuccess.ShouldBeTrue();
        summary.Value.PaymentStatus.ShouldBe(
            expected: nameof(PaymentStatus.Captured),
            customMessage: "a wallet captures immediately during InitiatePayment");
        summary.Value.PaymentMethod.ShouldBe(expected: nameof(PaymentMethod.Wallet));
    }

    private ValueTask<Result<PaymentDto>> GetByOrderAsync(Guid orderId) =>
        QueryHandler<GetPaymentByOrderQuery, PaymentDto>()
            .HandleAsync(query: new GetPaymentByOrderQuery(OrderId: orderId), cancellationToken: CancellationToken.None);
}
