// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Shouldly;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Definitions;

/// <summary>
/// <see cref="AgentOutputContract"/> keeps structured output opt-in and refuses shapes that cannot
/// carry one.
/// </summary>
public sealed class AgentOutputContractTests
{
    private sealed record ProductSummary(string Name, decimal Price);

    [Fact]
    public void None_Should_Not_Be_Declared()
    {
        // Prose is the common case, so the default must be the undeclared one.

        // Act & Assert
        AgentOutputContract.None.IsDeclared.ShouldBeFalse();
        AgentOutputContract.None.OutputType.ShouldBeNull();
    }

    [Fact]
    public void For_Should_Declare_TheGivenShape()
    {
        // Act
        var contract = AgentOutputContract.For<ProductSummary>();

        // Assert
        contract.IsDeclared.ShouldBeTrue();
        contract.OutputType.ShouldBe(expected: typeof(ProductSummary));
    }

    [Fact]
    public void Create_Should_Yield_None_When_NoTypeIsGiven()
    {
        // Act
        Result<AgentOutputContract> result = AgentOutputContract.Create(outputType: null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsDeclared.ShouldBeFalse();
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(DayOfWeek))]
    [InlineData(typeof(List<>))]
    public void Create_Should_Fail_When_TypeCannotCarryAStructuredAnswer(Type outputType)
    {
        // A schema needs named members to bind into. A model asked for a "schema" over a primitive,
        // a string or an open generic returns something that never binds, and the failure would only
        // show up at run time on the first structured call.

        // Act
        Result<AgentOutputContract> result = AgentOutputContract.Create(outputType: outputType);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "AgentOutputContract.OutputTypeNotSupported");
    }

    [Fact]
    public void Create_Should_Succeed_For_AClosedRecord()
    {
        // Act
        Result<AgentOutputContract> result = AgentOutputContract.Create(outputType: typeof(ProductSummary));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.OutputType.ShouldBe(expected: typeof(ProductSummary));
    }
}
