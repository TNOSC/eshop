// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// <see cref="PaymentFactory"/> — the "one payment per order" uniqueness rule, mirroring Catalog's
/// SKU-uniqueness test.
/// </summary>
public sealed class PaymentCreateTests
{
    [Fact]
    public async Task CreateAsync_Should_CreateAPendingPayment_When_NoneExistsForTheOrder()
    {
        // Arrange
        var orderId = Guid.CreateVersion7();
        IPaymentRepository repository = Substitute.For<IPaymentRepository>();
        repository
            .GetByOrderIdAsync(orderId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: null));

        // Act
        Result<PaymentAggregate> result = await PaymentFactory.CreateAsync(
            repository: repository,
            orderId: orderId,
            amount: Money.Create(amount: 20m, currency: "EUR").Value,
            method: PaymentMethod.Card);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.OrderId.ShouldBe(expected: orderId);
        result.Value.Status.ShouldBe(expected: PaymentStatus.Pending);
        result.Value.Method.ShouldBe(expected: PaymentMethod.Card);
    }

    [Fact]
    public async Task CreateAsync_Should_ReturnConflict_When_APaymentAlreadyExistsForTheOrder()
    {
        // Arrange
        var orderId = Guid.CreateVersion7();
        PaymentAggregate existing = await PaymentTestFactory.PendingAsync(orderId: orderId);

        IPaymentRepository repository = Substitute.For<IPaymentRepository>();
        repository
            .GetByOrderIdAsync(orderId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: existing));

        // Act
        Result<PaymentAggregate> result = await PaymentFactory.CreateAsync(
            repository: repository,
            orderId: orderId,
            amount: Money.Create(amount: 20m, currency: "EUR").Value,
            method: PaymentMethod.Card);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Payment.AlreadyExistsForOrder");
    }

    [Fact]
    public async Task CreateAsync_Should_ReturnValidation_When_TheOrderIdIsEmpty()
    {
        // Arrange
        IPaymentRepository repository = Substitute.For<IPaymentRepository>();

        // Act
        Result<PaymentAggregate> result = await PaymentFactory.CreateAsync(
            repository: repository,
            orderId: Guid.Empty,
            amount: Money.Create(amount: 20m, currency: "EUR").Value,
            method: PaymentMethod.Card);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Payment.OrderRequired");
        await repository.DidNotReceive().GetByOrderIdAsync(orderId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>());
    }
}
