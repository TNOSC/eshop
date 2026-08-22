// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Definitions;

/// <summary>
/// <see cref="Instructions"/> exists so an agent cannot be built without a persona.
/// </summary>
public sealed class InstructionsTests
{
    [Fact]
    public void Create_Should_Succeed_When_TextIsPresent()
    {
        // Act
        Result<Instructions> result = Instructions.Create(value: "You are a helpful assistant.");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: "You are a helpful assistant.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_TextIsBlank(string? value)
    {
        // An agent with no instructions still runs and still answers — with whatever persona the
        // model happens to default to. That failure is silent, which is exactly why it is refused
        // at construction instead.

        // Act
        Result<Instructions> result = Instructions.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Instructions.Empty");
    }

    [Fact]
    public void Create_Should_Fail_When_TextExceedsMaxLength()
    {
        // Arrange
        string tooLong = new(c: 'a', count: Instructions.MaxLength + 1);

        // Act
        Result<Instructions> result = Instructions.Create(value: tooLong);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Instructions.TooLong");
    }
}
