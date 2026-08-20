// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// <see cref="Email"/> owns its format invariant and the lowercase normalisation that makes email
/// uniqueness mean anything.
/// </summary>
public sealed class EmailTests
{
    [Theory]
    [InlineData("sami@example.com")]
    [InlineData("first.last@sub.domain.co.uk")]
    [InlineData("a+tag@example.org")]
    public void Create_Should_Succeed_When_TheAddressIsWellFormed(string value)
    {
        // Act
        Result<Email> result = Email.Create(value: value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: value);
    }

    // The whole point of normalising: without it, these two would be different customers to both the
    // domain uniqueness check and the unique index backing it.
    [Theory]
    [InlineData("Sami@Example.com", "sami@example.com")]
    [InlineData("  SAMI@EXAMPLE.COM  ", "sami@example.com")]
    public void Create_Should_NormalizeToLowercaseAndTrim(string value, string expected)
    {
        // Act
        Result<Email> result = Email.Create(value: value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: expected);
    }

    [Fact]
    public void Create_Should_ProduceEqualInstances_When_TheAddressesDifferOnlyByCase()
    {
        // Act
        Result<Email> lower = Email.Create(value: "sami@example.com");
        Result<Email> upper = Email.Create(value: "SAMI@EXAMPLE.COM");

        // Assert
        lower.Value.ShouldBe(expected: upper.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_ReturnEmpty_When_NoAddressIsSupplied(string? value)
    {
        // Act
        Result<Email> result = Email.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Email.Empty");
    }

    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("two@at@example.com")]
    [InlineData("sami@nodot")]
    [InlineData("sami@.example.com")]
    [InlineData("sami@example.com.")]
    [InlineData("sami with space@example.com")]
    public void Create_Should_ReturnInvalidFormat_When_TheAddressIsMalformed(string value)
    {
        // Act
        Result<Email> result = Email.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Email.InvalidFormat");
    }

    [Fact]
    public void Create_Should_ReturnTooLong_When_TheAddressExceedsTheMaxLength()
    {
        // Arrange
        string value = $"{new string(c: 'a', count: Email.MaxLength)}@example.com";

        // Act
        Result<Email> result = Email.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Email.TooLong");
    }
}
