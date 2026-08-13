// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Shouldly;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;
using Tnosc.EShop.Server.Domain.Shared;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// Each strategy's arithmetic in isolation — what a scheme pays out, independently of who qualifies
/// for it.
/// </summary>
public sealed class DiscountStrategyTests
{
    [Fact]
    public void NoDiscountStrategy_Should_ReturnTheSubtotalUnchanged()
    {
        // Arrange
        Money total = Amount(value: 123.45m);

        // Act
        Money discounted = new NoDiscountStrategy().Apply(total: total);

        // Assert
        discounted.Amount.ShouldBe(expected: 123.45m);
        discounted.Currency.ShouldBe(expected: total.Currency);
    }

    [Theory]
    [InlineData(100.00, 0.10, 90.00)]
    [InlineData(200.00, 0.05, 190.00)]
    [InlineData(49.99, 0.02, 48.99)]
    [InlineData(100.00, 0.00, 100.00)]
    [InlineData(100.00, 1.00, 0.00)]
    public void PercentageDiscountStrategy_Should_DeductTheFraction_And_RoundToTwoDecimals(
        decimal subtotal,
        decimal percentage,
        decimal expected)
    {
        // Act
        Money discounted = new PercentageDiscountStrategy(percentage: percentage).Apply(total: Amount(value: subtotal));

        // Assert
        discounted.Amount.ShouldBe(expected: expected);
    }

    [Fact]
    public void PercentageDiscountStrategy_Should_PreserveTheCurrency()
    {
        // Act
        Money discounted = new PercentageDiscountStrategy(percentage: 0.10m)
            .Apply(total: Money.Create(amount: 100m, currency: "TND").Value);

        // Assert
        discounted.Currency.ShouldBe(expected: "TND");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void PercentageDiscountStrategy_Should_Throw_When_TheFractionIsOutsideZeroToOne(decimal percentage)
    {
        // Act & Assert
        // A programming error, not a caller's input — nothing outside the domain constructs one of
        // these, so it throws rather than returning a Result nobody would be positioned to handle.
        Should.Throw<ArgumentOutOfRangeException>(actual: () => new PercentageDiscountStrategy(percentage: percentage));
    }

    [Theory]
    [InlineData(1000.00, 850.00)]
    [InlineData(1500.00, 1275.00)]
    [InlineData(500.00, 450.00)]
    [InlineData(999.99, 899.99)]
    [InlineData(499.99, 474.99)]
    [InlineData(10.00, 9.50)]
    public void TieredDiscountStrategy_Should_DeductTheBandTheSubtotalFallsIn(decimal subtotal, decimal expected)
    {
        // Act
        Money discounted = new TieredDiscountStrategy().Apply(total: Amount(value: subtotal));

        // Assert
        discounted.Amount.ShouldBe(expected: expected);
    }

    [Fact]
    public void TieredDiscountStrategy_Should_TreatTheBandBoundariesAsInclusive()
    {
        // Assert
        // The boundary is where a tiering bug hides, so it is asserted directly rather than inferred
        // from the amounts above.
        TieredDiscountStrategy.PercentageFor(amount: TieredDiscountStrategy.UpperBandThreshold)
            .ShouldBe(expected: TieredDiscountStrategy.UpperBandPercentage);
        TieredDiscountStrategy.PercentageFor(amount: TieredDiscountStrategy.UpperBandThreshold - 0.01m)
            .ShouldBe(expected: TieredDiscountStrategy.MiddleBandPercentage);
        TieredDiscountStrategy.PercentageFor(amount: TieredDiscountStrategy.MiddleBandThreshold)
            .ShouldBe(expected: TieredDiscountStrategy.MiddleBandPercentage);
        TieredDiscountStrategy.PercentageFor(amount: TieredDiscountStrategy.MiddleBandThreshold - 0.01m)
            .ShouldBe(expected: TieredDiscountStrategy.LowerBandPercentage);
    }

    [Fact]
    public void EveryStrategy_Should_NeverIncreaseTheSubtotal()
    {
        // Arrange
        Money total = Amount(value: 750.00m);

        IDiscountStrategy[] strategies =
        [
            new NoDiscountStrategy(),
            new PercentageDiscountStrategy(percentage: 0.10m),
            new TieredDiscountStrategy(),
        ];

        // Act & Assert
        foreach (IDiscountStrategy strategy in strategies)
        {
            Money discounted = strategy.Apply(total: total);

            discounted.Amount.ShouldBeLessThanOrEqualTo(
                expected: total.Amount,
                customMessage: $"{strategy.Name} must not charge more than the subtotal");
            discounted.Amount.ShouldBeGreaterThanOrEqualTo(
                expected: 0m,
                customMessage: $"{strategy.Name} must not produce a negative total");
        }
    }

    private static Money Amount(decimal value) =>
        Money.Create(amount: value, currency: "EUR").Value;
}
