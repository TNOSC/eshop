// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using Shouldly;
using Tnosc.EShop.Agent.Domain.Agents;
using Tnosc.Lib.Agent.Definitions;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Definitions;

/// <summary>
/// The shipped shopping assistant: it must construct, and it must not be able to write.
/// </summary>
public sealed class ShoppingAssistantDefinitionTests
{
    private static readonly AgentDefinition Definition = Resolve();

    [Fact]
    public void Definition_Should_Be_Named_As_The_Shared_Constant()
    {
        // The endpoint names the agent through this constant while the host registers it under the
        // same one. A drift between them is a 404 at run time, not a build error.

        // Act & Assert
        Definition.Name.Value.ShouldBe(expected: AgentNames.ShoppingAssistant);
    }

    [Fact]
    public void Definition_Should_Carry_Instructions_That_Forbid_Guessing()
    {
        // The single most damaging behaviour for a catalogue assistant is inventing a price or a
        // stock level, because a shopper cannot tell an invented answer from a looked-up one. The
        // prompt is free to be reworded; it is not free to drop that instruction.

        // Act & Assert
        Definition.Instructions.Value.ShouldContain(
            expected: "do not know",
            caseSensitivity: Case.Insensitive);
    }

    [Fact]
    public void Definition_Should_Not_Permit_AnyWriteTool()
    {
        // Defence in depth. The tool server checks the caller's own permissions, but an assistant
        // exposed to every signed-in shopper should not even be holding a write tool to be talked
        // into calling.

        // Act & Assert
        Definition.Tools.IsUnrestricted.ShouldBeFalse();

        Definition.Tools.Names.ShouldNotContain(
            elementPredicate: name =>
                name.Contains(value: "Create", comparisonType: StringComparison.OrdinalIgnoreCase) ||
                name.Contains(value: "Update", comparisonType: StringComparison.OrdinalIgnoreCase) ||
                name.Contains(value: "Delete", comparisonType: StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Definition_Should_Be_A_Chat_Agent_Answering_In_Prose()
    {
        // Act & Assert
        Definition.Kind.ShouldBe(expected: AgentKind.Chat);
        Definition.Output.IsDeclared.ShouldBeFalse();
    }

    private static AgentDefinition Resolve()
    {
        // The definition type is internal to the domain project, so it is reached the same way the
        // host reaches it: through the marker interface every agent is discovered by.
        Type providerType = typeof(AgentNames).Assembly
            .GetTypes()
            .Single(predicate: static type =>
                type is { IsAbstract: false, IsInterface: false } &&
                typeof(IAgentDefinitionProvider).IsAssignableFrom(c: type));

        var provider = (IAgentDefinitionProvider)Activator.CreateInstance(
            type: providerType,
            nonPublic: true)!;

        return provider.Definition;
    }
}
