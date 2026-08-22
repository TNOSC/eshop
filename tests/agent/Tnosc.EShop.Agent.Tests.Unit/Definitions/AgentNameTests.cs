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
/// <see cref="AgentName"/> owns the format that lets a name be both a route segment and a DI key.
/// </summary>
public sealed class AgentNameTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("shopping-assistant")]
    [InlineData("agent-2")]
    [InlineData("order-status-checker")]
    public void Create_Should_Succeed_When_ValueIsLowercaseKebabCase(string value)
    {
        // Act
        Result<AgentName> result = AgentName.Create(value: value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected: value);
    }

    [Theory]
    [InlineData(null, "AgentName.Empty")]
    [InlineData("", "AgentName.Empty")]
    [InlineData("   ", "AgentName.Empty")]
    [InlineData("Shopping-Assistant", "AgentName.InvalidFormat")]
    [InlineData("shopping_assistant", "AgentName.InvalidFormat")]
    [InlineData("shopping assistant", "AgentName.InvalidFormat")]
    [InlineData("shopping/assistant", "AgentName.InvalidFormat")]
    public void Create_Should_Fail_When_ValueBreaksTheFormatInvariant(string? value, string expectedCode)
    {
        // Act
        Result<AgentName> result = AgentName.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: expectedCode);
    }

    [Theory]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    public void Create_Should_Fail_When_ValueStartsOrEndsWithADash(string value)
    {
        // A dash on either end would produce an empty route segment or a double slash once the name
        // is composed into a path, so it is rejected here rather than at routing time.

        // Act
        Result<AgentName> result = AgentName.Create(value: value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "AgentName.InvalidFormat");
    }

    [Fact]
    public void Create_Should_Fail_When_ValueExceedsMaxLength()
    {
        // Arrange
        string tooLong = new(c: 'a', count: AgentName.MaxLength + 1);

        // Act
        Result<AgentName> result = AgentName.Create(value: tooLong);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "AgentName.TooLong");
    }
}
