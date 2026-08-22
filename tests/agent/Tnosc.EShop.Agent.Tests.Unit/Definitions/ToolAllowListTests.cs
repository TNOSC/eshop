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
/// <see cref="ToolAllowList"/> decides what an agent may reach for.
/// </summary>
public sealed class ToolAllowListTests
{
    [Fact]
    public void Create_Should_Yield_Unrestricted_When_NoNamesAreGiven()
    {
        // Act
        Result<ToolAllowList> result = ToolAllowList.Create(names: []);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsUnrestricted.ShouldBeTrue();
    }

    [Fact]
    public void Permits_Should_Allow_Everything_When_Unrestricted()
    {
        // Act & Assert
        ToolAllowList.Unrestricted.Permits(toolName: "anything-at-all").ShouldBeTrue();
    }

    [Fact]
    public void Permits_Should_Allow_Only_NamedTools_When_Restricted()
    {
        // Arrange
        ToolAllowList allowList = ToolAllowList.Create(names: ["ListProducts"]).Value;

        // Act & Assert
        allowList.Permits(toolName: "ListProducts").ShouldBeTrue();
        allowList.Permits(toolName: "CreateProduct").ShouldBeFalse();
    }

    [Fact]
    public void Permits_Should_Be_CaseSensitive()
    {
        // Tool names come from a server and are matched ordinally. A near-miss that "works" locally
        // and fails against a differently-cased server would be worse than a clean miss here.

        // Arrange
        ToolAllowList allowList = ToolAllowList.Create(names: ["ListProducts"]).Value;

        // Act & Assert
        allowList.Permits(toolName: "listproducts").ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_AToolNameIsBlank(string blank)
    {
        // Act
        Result<ToolAllowList> result = ToolAllowList.Create(names: ["ListProducts", blank]);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "ToolAllowList.NameEmpty");
    }

    [Fact]
    public void Create_Should_Fail_When_AToolNameIsRepeated()
    {
        // Rejected rather than de-duplicated: a repeat usually means two people edited the list
        // without seeing each other's entry, and collapsing it silently hides that.

        // Act
        Result<ToolAllowList> result = ToolAllowList.Create(names: ["ListProducts", "ListProducts"]);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "ToolAllowList.Duplicate");
    }

    [Fact]
    public void Equality_Should_Compare_By_Value()
    {
        // The allow-list is held by a record, so it must not be backed by a mutable collection —
        // that would compare by reference and silently break every value-object comparison it is
        // part of.

        // Arrange
        ToolAllowList first = ToolAllowList.Create(names: ["A", "B"]).Value;
        ToolAllowList second = ToolAllowList.Create(names: ["A", "B"]).Value;

        // Act & Assert
        first.ShouldBe(expected: second);
    }
}
