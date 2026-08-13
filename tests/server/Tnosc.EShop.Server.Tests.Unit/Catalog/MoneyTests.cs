// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Bogus;
using Shouldly;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// <see cref="Money"/> owns "an amount is never negative" and "a currency is a three-letter ISO code".
/// Neither rule may be re-stated in a validator or a handler.
/// </summary>
public sealed class MoneyTests
{
    private readonly Faker _faker = CatalogFaker.New();

    [Theory]
    [InlineData(0, "EUR")]
    [InlineData(0.01, "USD")]
    [InlineData(19.99, "TND")]
    public void Create_Should_Succeed_When_AmountIsNotNegativeAndCurrencyIsIso(decimal amount, string currency)
    {
        // Act
        Result<Money> result = Money.Create(amount: amount, currency: currency);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(expected: amount);
        result.Value.Currency.ShouldBe(expected: currency);
    }

    [Fact]
    public void Create_Should_ReturnValidationError_When_AmountIsNegative()
    {
        // Arrange
        decimal amount = -_faker.PriceAmount();

        // Act
        Result<Money> result = Money.Create(amount: amount, currency: _faker.Currency());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Money.NegativeAmount");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("eur")]
    [InlineData("E1R")]
    public void Create_Should_ReturnValidationError_When_CurrencyIsNotAThreeLetterUppercaseCode(string? currency)
    {
        // Act
        Result<Money> result = Money.Create(amount: 10m, currency: currency);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Money.InvalidCurrency");
    }

    [Fact]
    public void Equals_Should_BeFalse_When_CurrenciesDiffer()
    {
        // Arrange
        decimal amount = _faker.PriceAmount();

        // Act
        Money euros = Money.Create(amount: amount, currency: "EUR").Value;
        Money dollars = Money.Create(amount: amount, currency: "USD").Value;

        // Assert
        euros.ShouldNotBe(expected: dollars);
    }
}
