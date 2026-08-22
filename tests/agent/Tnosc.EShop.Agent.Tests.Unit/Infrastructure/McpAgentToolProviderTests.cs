// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Agent.Infrastructure.Mcp;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Agent.Definitions;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Infrastructure;

/// <summary>
/// <see cref="McpAgentToolProvider.Filter"/> narrows what the server exposes to what an agent's
/// allow-list permits — this is the check that would have caught the shopping assistant's allow-list
/// naming a tool the server never exposed under that name.
/// </summary>
public sealed class McpAgentToolProviderTests
{
    [Fact]
    public void Filter_Should_Keep_Only_TheAllowListedTool()
    {
        // Arrange
        AgentDefinition definition = AgentTestFactory.Definition(tools: [McpToolNames.ListProducts]);
        IReadOnlyList<McpClientTool> available =
        [
            ToolNamed(name: McpToolNames.ListProducts),
            ToolNamed(name: McpToolNames.CreateProduct),
        ];

        // Act
        IReadOnlyList<AITool> filtered = McpAgentToolProvider.Filter(
            definition: definition,
            available: available,
            logger: NullLogger.Instance);

        // Assert
        filtered.ShouldHaveSingleItem().Name.ShouldBe(expected: McpToolNames.ListProducts);
    }

    [Fact]
    public void Filter_Should_Keep_Everything_When_Unrestricted()
    {
        // Arrange
        AgentDefinition definition = AgentTestFactory.Definition(tools: null);
        IReadOnlyList<McpClientTool> available =
        [
            ToolNamed(name: McpToolNames.ListProducts),
            ToolNamed(name: McpToolNames.CreateProduct),
        ];

        // Act
        IReadOnlyList<AITool> filtered = McpAgentToolProvider.Filter(
            definition: definition,
            available: available,
            logger: NullLogger.Instance);

        // Assert
        filtered.Count.ShouldBe(expected: 2);
    }

    [Fact]
    public void Filter_Should_Drop_ANamedTool_The_ServerDoesNotExpose()
    {
        // A tool named in the allow-list but missing from the server's answer is not an error — it
        // is logged and silently absent, which is the exact failure mode a mismatched literal used
        // to produce for the shopping assistant before its tool name was shared via McpToolNames.

        // Arrange
        AgentDefinition definition = AgentTestFactory.Definition(tools: [McpToolNames.ListProducts]);
        IReadOnlyList<McpClientTool> available = [ToolNamed(name: McpToolNames.CreateProduct)];

        // Act
        IReadOnlyList<AITool> filtered = McpAgentToolProvider.Filter(
            definition: definition,
            available: available,
            logger: NullLogger.Instance);

        // Assert
        filtered.ShouldBeEmpty();
    }

    private static McpClientTool ToolNamed(string name) =>
        new(client: Substitute.For<McpClient>(),
            tool: new Tool { Name = name },
            serializerOptions: null);
}
