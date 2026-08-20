// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// Each <see cref="IPaymentMethodStrategy"/>'s own settlement plan.
/// </summary>
public sealed class PaymentMethodStrategyTests
{
    [Fact]
    public void CardStrategy_Should_PlanTwoStepSettlement()
    {
        // Arrange
        var strategy = new CardPaymentMethodStrategy();
        Money amount = Money.Create(amount: 100m, currency: "EUR").Value;

        // Act
        Result<PaymentPlan> plan = strategy.Plan(amount: amount);

        // Assert
        plan.IsSuccess.ShouldBeTrue();
        plan.Value.RequiresAuthorization.ShouldBeTrue();
        plan.Value.CapturesImmediately.ShouldBeFalse();
    }

    [Fact]
    public void WalletStrategy_Should_PlanImmediateCapture()
    {
        // Arrange
        var strategy = new WalletPaymentMethodStrategy();
        Money amount = Money.Create(amount: 100m, currency: "EUR").Value;

        // Act
        Result<PaymentPlan> plan = strategy.Plan(amount: amount);

        // Assert
        plan.IsSuccess.ShouldBeTrue();
        plan.Value.RequiresAuthorization.ShouldBeFalse();
        plan.Value.CapturesImmediately.ShouldBeTrue();
    }

    [Fact]
    public void CashOnDeliveryStrategy_Should_PlanDeferredSettlement_When_WithinTheLimit()
    {
        // Arrange
        var strategy = new CashOnDeliveryPaymentMethodStrategy();
        Money amount = Money.Create(amount: 100m, currency: "EUR").Value;

        // Act
        Result<PaymentPlan> plan = strategy.Plan(amount: amount);

        // Assert
        plan.IsSuccess.ShouldBeTrue();
        plan.Value.RequiresAuthorization.ShouldBeFalse();
        plan.Value.CapturesImmediately.ShouldBeFalse();
    }

    [Fact]
    public void CashOnDeliveryStrategy_Should_ReturnConflict_When_TheAmountExceedsTheLimit()
    {
        // Arrange
        var strategy = new CashOnDeliveryPaymentMethodStrategy();
        Money amount = Money.Create(amount: CashOnDeliveryPaymentMethodStrategy.Limit + 0.01m, currency: "EUR").Value;

        // Act
        Result<PaymentPlan> plan = strategy.Plan(amount: amount);

        // Assert
        plan.IsError.ShouldBeTrue();
        plan.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        plan.FirstError.Code.ShouldBe(expected: "Payment.CashOnDeliveryLimitExceeded");
    }

    [Fact]
    public void EveryStrategy_Should_RejectAZeroAmount()
    {
        // Arrange
        Money zero = Money.Create(amount: 0m, currency: "EUR").Value;

        // Act
        Result<PaymentPlan> cardPlan = new CardPaymentMethodStrategy().Plan(amount: zero);
        Result<PaymentPlan> walletPlan = new WalletPaymentMethodStrategy().Plan(amount: zero);
        Result<PaymentPlan> codPlan = new CashOnDeliveryPaymentMethodStrategy().Plan(amount: zero);

        // Assert
        cardPlan.IsError.ShouldBeTrue();
        cardPlan.FirstError.Code.ShouldBe(expected: "Payment.AmountMustBePositive");
        walletPlan.IsError.ShouldBeTrue();
        codPlan.IsError.ShouldBeTrue();
    }
}
