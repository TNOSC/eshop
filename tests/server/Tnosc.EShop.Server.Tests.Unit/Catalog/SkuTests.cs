// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Bogus;
using Shouldly;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// <see cref="Sku"/> owns its own format invariant; uniqueness is not its business.
/// </summary>
public sealed class SkuTests
{
    private readonly Faker _faker = CatalogFaker.New();

    [Theory]
    [InlineData("A")]
    [InlineData("WIDGET-1")]
    [InlineData("ABC-123-XYZ")]
    [InlineData("12345678901234567890123456789012")]
    public void Create_Should_Succeed_When_ValueIsUppercaseAlphanumericWithDashes(string value)
    {
        // Act
        Result<Sku> result = Sku.Create(value: value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: value);
    }

    [Theory]
    [InlineData(null, "Sku.Empty")]
    [InlineData("", "Sku.Empty")]
    [InlineData("   ", "Sku.Empty")]
    [InlineData("123456789012345678901234567890123", "Sku.TooLong")]
    [InlineData("widget-1", "Sku.InvalidFormat")]
    [InlineData("WIDGET 1", "Sku.InvalidFormat")]
    [InlineData("WIDGET_1", "Sku.InvalidFormat")]
    [InlineData("WIDGET#1", "Sku.InvalidFormat")]
    public void Create_Should_ReturnValidationError_When_ValueBreaksTheFormatInvariant(string? value, string expectedCode)
    {
        // Act
        Result<Sku> result = Sku.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: expectedCode);
    }

    [Fact]
    public void Equals_Should_BeTrue_When_TwoSkusHoldTheSameValue()
    {
        // Arrange
        string value = _faker.Sku();

        // Act
        Sku first = Sku.Create(value: value).Value;
        Sku second = Sku.Create(value: value).Value;

        // Assert
        first.ShouldBe(expected: second);
    }
}
