// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;
using Tnosc.EShop.Server.Domain.Shared;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// The selection rule — which scheme a given tier and subtotal qualifies for. The counterpart to
/// <see cref="DiscountStrategyTests"/>, which covers what each scheme then pays.
/// </summary>
public sealed class DiscountStrategyFactoryTests
{
    [Fact]
    public void Create_Should_ChooseTiered_For_AGoldCustomer_WhateverTheSubtotal()
    {
        // Act
        IDiscountStrategy small = Create(subtotal: 1.00m, tier: CustomerTier.Gold);
        IDiscountStrategy large = Create(subtotal: 5000.00m, tier: CustomerTier.Gold);

        // Assert
        small.ShouldBeOfType<TieredDiscountStrategy>();
        large.ShouldBeOfType<TieredDiscountStrategy>();
    }

    [Fact]
    public void Create_Should_ChooseAFlatPercentage_For_ASilverCustomer_WhateverTheSubtotal()
    {
        // Act
        IDiscountStrategy strategy = Create(subtotal: 1.00m, tier: CustomerTier.Silver);

        // Assert
        strategy.ShouldBeOfType<PercentageDiscountStrategy>()
            .Percentage.ShouldBe(expected: DiscountStrategyFactory.SilverTierPercentage);
    }

    [Fact]
    public void Create_Should_ChooseAFlatPercentage_For_AStandardCustomer_Spending_PastTheThreshold()
    {
        // Act
        IDiscountStrategy strategy = Create(
            subtotal: DiscountStrategyFactory.StandardTierThreshold,
            tier: CustomerTier.Standard);

        // Assert
        strategy.ShouldBeOfType<PercentageDiscountStrategy>()
            .Percentage.ShouldBe(expected: DiscountStrategyFactory.StandardTierPercentage);
    }

    [Fact]
    public void Create_Should_ChooseNoDiscount_For_AStandardCustomer_Below_TheThreshold()
    {
        // Act
        IDiscountStrategy strategy = Create(
            subtotal: DiscountStrategyFactory.StandardTierThreshold - 0.01m,
            tier: CustomerTier.Standard);

        // Assert
        // Never null: the caller has no absent case to branch on, which is why Order.Create can take
        // the strategy unconditionally.
        strategy.ShouldBeOfType<NoDiscountStrategy>();
    }

    [Theory]
    [InlineData(0, CustomerTier.Standard)]
    [InlineData(2, CustomerTier.Standard)]
    [InlineData(3, CustomerTier.Silver)]
    [InlineData(9, CustomerTier.Silver)]
    [InlineData(10, CustomerTier.Gold)]
    [InlineData(250, CustomerTier.Gold)]
    public void CustomerTierFactory_Should_MapAnOrderCountOntoItsTier(int previousOrderCount, CustomerTier expected)
    {
        // Act
        CustomerTier tier = CustomerTierFactory.For(previousOrderCount: previousOrderCount);

        // Assert
        tier.ShouldBe(expected: expected);
    }

    private static IDiscountStrategy Create(decimal subtotal, CustomerTier tier) =>
        DiscountStrategyFactory.Create(
            total: Money.Create(amount: subtotal, currency: "EUR").Value,
            tier: tier);
}
