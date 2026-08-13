// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Shouldly;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// Ordering's value objects: what each <c>Create</c> accepts, what it rejects, and with which
/// <c>ErrorType</c>.
/// </summary>
public sealed class OrderingValueObjectTests
{
    [Fact]
    public void OrderNumber_Generate_Should_ProduceTheDocumentedShape()
    {
        // Act
        var number = OrderNumber.Generate(placedOnUtc: new DateTime(year: 2026, month: 8, day: 13, hour: 12, minute: 0, second: 0, kind: DateTimeKind.Utc));

        // Assert
        number.Value.ShouldStartWith(expected: "ORD-20260813-");
        number.Value.Length.ShouldBe(expected: OrderNumber.Length);
    }

    [Fact]
    public void OrderNumber_Generate_Should_ProduceDistinctValues_ForTheSameInstant()
    {
        // Arrange
        var instant = new DateTime(year: 2026, month: 8, day: 13, hour: 12, minute: 0, second: 0, kind: DateTimeKind.Utc);

        // Act
        var first = OrderNumber.Generate(placedOnUtc: instant);
        var second = OrderNumber.Generate(placedOnUtc: instant);

        // Assert
        // Two orders in the same second must not share a customer-facing reference. The random suffix
        // is what separates them; the unique index is the backstop behind it.
        second.ShouldNotBe(expected: first);
    }

    [Theory]
    [InlineData("ORD-20260813-A1B2C3")]
    [InlineData("ORD-20260101-000000")]
    [InlineData("ORD-20261231-ZZZZZZ")]
    public void OrderNumber_Create_Should_Accept_AWellFormedValue(string value)
    {
        // Act
        Result<OrderNumber> number = OrderNumber.Create(value: value);

        // Assert
        number.IsSuccess.ShouldBeTrue();
        number.Value.Value.ShouldBe(expected: value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void OrderNumber_Create_Should_Reject_AMissingValue(string? value)
    {
        // Act
        Result<OrderNumber> number = OrderNumber.Create(value: value);

        // Assert
        number.IsError.ShouldBeTrue();
        number.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        number.FirstError.Code.ShouldBe(expected: "OrderNumber.Required");
    }

    [Theory]
    [InlineData("ORD-20260813-A1B2C")]
    [InlineData("ORD-20260813-A1B2C34")]
    [InlineData("XXX-20260813-A1B2C3")]
    [InlineData("ORD-2026081X-A1B2C3")]
    [InlineData("ORD-20260813-a1b2c3")]
    [InlineData("ORD_20260813_A1B2C3")]
    public void OrderNumber_Create_Should_Reject_AMalformedValue(string value)
    {
        // Act
        Result<OrderNumber> number = OrderNumber.Create(value: value);

        // Assert
        number.IsError.ShouldBeTrue();
        number.FirstError.Code.ShouldBe(expected: "OrderNumber.InvalidFormat");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void OrderQuantity_Create_Should_Accept_AValueInsideTheRange(int value)
    {
        // Act
        Result<OrderQuantity> quantity = OrderQuantity.Create(value: value);

        // Assert
        quantity.IsSuccess.ShouldBeTrue();
        quantity.Value.Value.ShouldBe(expected: value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void OrderQuantity_Create_Should_Reject_AValueOutsideTheRange(int value)
    {
        // Act
        Result<OrderQuantity> quantity = OrderQuantity.Create(value: value);

        // Assert
        quantity.IsError.ShouldBeTrue();
        quantity.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        quantity.FirstError.Code.ShouldBe(expected: "OrderQuantity.OutOfRange");
    }

    [Fact]
    public void ShippingAddress_Create_Should_TrimAndUppercaseTheCountry()
    {
        // Act
        Result<ShippingAddress> address = ShippingAddress.Create(
            street: "  1 Rue de Carthage  ",
            city: " Tunis ",
            postalCode: " 1000 ",
            country: " tn ");

        // Assert
        address.IsSuccess.ShouldBeTrue();
        address.Value.Street.ShouldBe(expected: "1 Rue de Carthage");
        address.Value.City.ShouldBe(expected: "Tunis");
        address.Value.PostalCode.ShouldBe(expected: "1000");
        address.Value.Country.ShouldBe(expected: "TN");
    }

    [Theory]
    [InlineData(null, "Tunis", "1000", "TN", "ShippingAddress.InvalidStreet")]
    [InlineData("  ", "Tunis", "1000", "TN", "ShippingAddress.InvalidStreet")]
    [InlineData("1 Rue", null, "1000", "TN", "ShippingAddress.InvalidCity")]
    [InlineData("1 Rue", "Tunis", null, "TN", "ShippingAddress.InvalidPostalCode")]
    [InlineData("1 Rue", "Tunis", "1000", null, "ShippingAddress.InvalidCountry")]
    [InlineData("1 Rue", "Tunis", "1000", "TUN", "ShippingAddress.InvalidCountry")]
    [InlineData("1 Rue", "Tunis", "1000", "T1", "ShippingAddress.InvalidCountry")]
    public void ShippingAddress_Create_Should_Reject_AnIncompleteOrMalformedAddress(
        string? street,
        string? city,
        string? postalCode,
        string? country,
        string expectedCode)
    {
        // Act
        Result<ShippingAddress> address = ShippingAddress.Create(
            street: street,
            city: city,
            postalCode: postalCode,
            country: country);

        // Assert
        address.IsError.ShouldBeTrue();
        address.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        address.FirstError.Code.ShouldBe(expected: expectedCode);
    }

    [Fact]
    public void ShippingAddress_Should_CompareByValue()
    {
        // Act
        ShippingAddress first = OrderTestFactory.Address();
        ShippingAddress second = OrderTestFactory.Address();

        // Assert
        second.ShouldBe(expected: first);
    }
}
