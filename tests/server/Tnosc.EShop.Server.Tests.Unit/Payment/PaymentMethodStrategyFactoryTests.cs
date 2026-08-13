// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// <see cref="PaymentMethodStrategyFactory"/> — the sole place a payment method is mapped to how it
/// settles.
/// </summary>
public sealed class PaymentMethodStrategyFactoryTests
{
    [Fact]
    public void Create_Should_ReturnCardStrategy_When_TheMethodIsCard()
    {
        // Act
        IPaymentMethodStrategy strategy = PaymentMethodStrategyFactory.Create(method: PaymentMethod.Card);

        // Assert
        strategy.ShouldBeOfType<CardPaymentMethodStrategy>();
        strategy.RequiresAuthorization.ShouldBeTrue();
    }

    [Fact]
    public void Create_Should_ReturnWalletStrategy_When_TheMethodIsWallet()
    {
        // Act
        IPaymentMethodStrategy strategy = PaymentMethodStrategyFactory.Create(method: PaymentMethod.Wallet);

        // Assert
        strategy.ShouldBeOfType<WalletPaymentMethodStrategy>();
        strategy.RequiresAuthorization.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_ReturnCashOnDeliveryStrategy_When_TheMethodIsCashOnDelivery()
    {
        // Act
        IPaymentMethodStrategy strategy = PaymentMethodStrategyFactory.Create(method: PaymentMethod.CashOnDelivery);

        // Assert
        strategy.ShouldBeOfType<CashOnDeliveryPaymentMethodStrategy>();
        strategy.RequiresAuthorization.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_Throw_When_TheMethodIsNotDefined()
    {
        // Act
        Should.Throw<System.ArgumentOutOfRangeException>(actual: () =>
            PaymentMethodStrategyFactory.Create(method: (PaymentMethod)999));
    }
}
