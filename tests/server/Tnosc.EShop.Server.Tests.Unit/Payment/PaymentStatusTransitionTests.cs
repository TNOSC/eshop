// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;
using Shouldly;
using Tnosc.EShop.Server.Domain.Payment.Payments.Events;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// Every legal and illegal <see cref="PaymentAggregate"/> status transition.
/// </summary>
public sealed class PaymentStatusTransitionTests
{
    [Fact]
    public async Task Authorize_Should_Succeed_When_ThePaymentIsPending()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.PendingAsync();

        // Act
        Result result = payment.Authorize(gatewayReference: "auth_123");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        payment.GatewayReference.ShouldBe(expected: "auth_123");
    }

    [Fact]
    public async Task Authorize_Should_ReturnConflict_When_ThePaymentIsAlreadyAuthorized()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.AuthorizedAsync();

        // Act
        Result result = payment.Authorize(gatewayReference: "auth_456");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Payment.CannotAuthorize");
    }

    [Fact]
    public async Task Capture_Should_Succeed_When_ThePaymentIsPending()
    {
        // Arrange — wallet/cash-on-delivery shape: captures straight from Pending.
        PaymentAggregate payment = await PaymentTestFactory.PendingAsync();

        // Act
        Result result = payment.Capture(gatewayReference: "cap_123");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Capture_Should_Succeed_When_ThePaymentIsAuthorized()
    {
        // Arrange — card shape: capture follows a prior authorization.
        PaymentAggregate payment = await PaymentTestFactory.AuthorizedAsync();

        // Act
        Result result = payment.Capture(gatewayReference: "cap_123");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        payment.GatewayReference.ShouldBe(expected: "cap_123");
    }

    [Fact]
    public async Task Capture_Should_RaisePaymentSucceeded()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.AuthorizedAsync();

        // Act
        payment.Capture(gatewayReference: "cap_123");

        // Assert
        payment.DomainEvents.ShouldContain(elementPredicate: domainEvent =>
            domainEvent is PaymentSucceededDomainEvent);
    }

    [Fact]
    public async Task Capture_Should_ReturnConflict_When_ThePaymentIsAlreadyCaptured()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.CapturedAsync();

        // Act
        Result result = payment.Capture(gatewayReference: "cap_456");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Payment.CannotCapture");
    }

    [Fact]
    public async Task Fail_Should_Succeed_And_RaisePaymentFailed_When_ThePaymentIsPending()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.PendingAsync();

        // Act
        Result result = payment.Fail(reason: "card_declined");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        payment.FailureReason.ShouldBe(expected: "card_declined");
        payment.DomainEvents.ShouldContain(elementPredicate: domainEvent =>
            domainEvent is PaymentFailedDomainEvent);
    }

    [Fact]
    public async Task Fail_Should_ReturnConflict_When_ThePaymentIsAlreadyCaptured()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.CapturedAsync();

        // Act
        Result result = payment.Fail(reason: "too late");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Payment.CannotFail");
    }

    [Fact]
    public async Task Refund_Should_Succeed_When_ThePaymentIsCaptured()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.CapturedAsync();

        // Act
        Result result = payment.Refund(reason: "customer request");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        payment.DomainEvents.ShouldContain(elementPredicate: domainEvent =>
            domainEvent is PaymentRefundedDomainEvent);
    }

    [Fact]
    public async Task Refund_Should_ReturnConflict_When_ThePaymentWasNeverCaptured()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.PendingAsync();

        // Act
        Result result = payment.Refund(reason: "customer request");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Payment.CannotRefund");
    }

    [Fact]
    public async Task Refund_Should_ReturnConflict_When_ThePaymentIsAlreadyRefunded()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.RefundedAsync();

        // Act
        Result result = payment.Refund(reason: "again");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Payment.CannotRefund");
    }
}
