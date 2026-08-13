// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// The invariants <see cref="ExternalUserId"/>, <see cref="PersonName"/> and <see cref="PhoneNumber"/>
/// own, each enforced in its own <c>Create</c> factory rather than by a validator.
/// </summary>
public sealed class IdentityValueObjectTests
{
    [Fact]
    public void ExternalUserId_Create_Should_Succeed_And_Trim()
    {
        // Act
        Result<ExternalUserId> result = ExternalUserId.Create(value: "  keycloak-sub  ");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: "keycloak-sub");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExternalUserId_Create_Should_ReturnEmpty_When_NothingIsSupplied(string? value)
    {
        // Act
        Result<ExternalUserId> result = ExternalUserId.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "ExternalUserId.Empty");
    }

    [Fact]
    public void ExternalUserId_Create_Should_ReturnTooLong_When_ItExceedsTheMaxLength()
    {
        // Act
        Result<ExternalUserId> result = ExternalUserId.Create(value: new string(c: 'a', count: ExternalUserId.MaxLength + 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "ExternalUserId.TooLong");
    }

    [Fact]
    public void PersonName_Create_Should_Succeed_And_Trim()
    {
        // Act
        Result<PersonName> result = PersonName.Create(firstName: "  Sami  ", lastName: "  Shopper  ");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.FirstName.ShouldBe(expected: "Sami");
        result.Value.LastName.ShouldBe(expected: "Shopper");
    }

    [Theory]
    [InlineData(null, "Shopper", "PersonName.FirstNameRequired")]
    [InlineData("   ", "Shopper", "PersonName.FirstNameRequired")]
    [InlineData("Sami", null, "PersonName.LastNameRequired")]
    [InlineData("Sami", "   ", "PersonName.LastNameRequired")]
    public void PersonName_Create_Should_ReturnRequired_When_APartIsMissing(
        string? firstName,
        string? lastName,
        string expectedCode)
    {
        // Act
        Result<PersonName> result = PersonName.Create(firstName: firstName, lastName: lastName);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: expectedCode);
    }

    [Fact]
    public void PersonName_Create_Should_ReturnTooLong_When_APartExceedsTheMaxLength()
    {
        // Arrange
        string tooLong = new(c: 'A', count: PersonName.MaxPartLength + 1);

        // Act
        Result<PersonName> first = PersonName.Create(firstName: tooLong, lastName: "Shopper");
        Result<PersonName> last = PersonName.Create(firstName: "Sami", lastName: tooLong);

        // Assert
        first.FirstError.Code.ShouldBe(expected: "PersonName.FirstNameTooLong");
        last.FirstError.Code.ShouldBe(expected: "PersonName.LastNameTooLong");
    }

    [Theory]
    [InlineData("+21612345678", "+21612345678")]
    [InlineData("+216 12 345 678", "+21612345678")]
    [InlineData("+1 (555) 010-9999", "+15550109999")]
    public void PhoneNumber_Create_Should_StripSeparators_And_KeepE164(string value, string expected)
    {
        // Act
        Result<PhoneNumber> result = PhoneNumber.Create(value: value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: expected);
    }

    [Theory]
    [InlineData("21612345678")]
    [InlineData("+123")]
    [InlineData("+1234567890123456")]
    [InlineData("+21612345abc")]
    public void PhoneNumber_Create_Should_ReturnInvalidFormat_When_ItIsNotE164(string value)
    {
        // Act
        Result<PhoneNumber> result = PhoneNumber.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "PhoneNumber.InvalidFormat");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PhoneNumber_Create_Should_ReturnEmpty_When_ABlankValueIsSupplied(string? value)
    {
        // Act
        Result<PhoneNumber> result = PhoneNumber.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "PhoneNumber.Empty");
    }
}
