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
/// <see cref="AgentDefinition"/> validates the whole of an agent, not just its parts.
/// </summary>
public sealed class AgentDefinitionTests
{
    [Fact]
    public void Create_Should_Succeed_When_EveryPartIsValid()
    {
        // Act
        Result<AgentDefinition> result = Create(description: "Answers catalogue questions.");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.Value.ShouldBe(expected: "test-agent");
        result.Value.Kind.ShouldBe(expected: AgentKind.Chat);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_DescriptionIsBlank(string description)
    {
        // The description is what a delegating caller reads when deciding whether to hand work to
        // this agent, so an empty one makes the agent undiscoverable rather than merely undocumented.

        // Act
        Result<AgentDefinition> result = Create(description: description);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "AgentDefinition.DescriptionEmpty");
    }

    private static Result<AgentDefinition> Create(string description) =>
        AgentDefinition.Create(
            name: AgentName.Create(value: "test-agent").Value,
            description: description,
            kind: AgentKind.Chat,
            instructions: Instructions.Create(value: "Be helpful.").Value,
            tools: ToolAllowList.Unrestricted,
            model: ModelParameters.Default,
            output: AgentOutputContract.None);
}
