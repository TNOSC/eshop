// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Bogus;
using Shouldly;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// <see cref="StockQuantity"/> owns "stock never goes negative", on creation and on adjustment alike.
/// </summary>
public sealed class StockQuantityTests
{
    private readonly Faker _faker = CatalogFaker.New();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Create_Should_Succeed_When_ValueIsNotNegative(int value)
    {
        // Act
        Result<StockQuantity> result = StockQuantity.Create(value: value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: value);
    }

    [Fact]
    public void Create_Should_ReturnValidationError_When_ValueIsNegative()
    {
        // Arrange
        int value = -_faker.Random.Int(min: 1, max: 1000);

        // Act
        Result<StockQuantity> result = StockQuantity.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "StockQuantity.Negative");
    }

    [Fact]
    public void Adjust_Should_ReturnTheNewQuantity_When_TheResultStaysAtOrAboveZero()
    {
        // Arrange
        int initial = _faker.Random.Int(min: 5, max: 1000);
        int increaseBy = _faker.Random.Int(min: 1, max: 100);
        StockQuantity quantity = StockQuantity.Create(value: initial).Value;

        // Act
        Result<StockQuantity> increased = quantity.Adjust(delta: increaseBy);
        Result<StockQuantity> emptied = quantity.Adjust(delta: -initial);

        // Assert
        increased.Value.Value.ShouldBe(expected: initial + increaseBy);
        emptied.Value.Value.ShouldBe(expected: 0);
    }

    [Fact]
    public void Adjust_Should_ReturnValidationError_When_TheResultWouldGoBelowZero()
    {
        // Arrange
        int initial = _faker.Random.Int(min: 1, max: 1000);
        StockQuantity quantity = StockQuantity.Create(value: initial).Value;

        // Act
        Result<StockQuantity> result = quantity.Adjust(delta: -(initial + 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "StockQuantity.Negative");
    }
}
