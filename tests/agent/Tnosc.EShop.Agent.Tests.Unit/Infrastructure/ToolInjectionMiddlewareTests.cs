// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Agent.Infrastructure.Ai.Middleware;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Runtime;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Infrastructure;

/// <summary>
/// A client-declared AG-UI tool (e.g. a render-only frontend tool such as
/// <c>render_product_card</c>) may already be on the caller's <c>ChatOptions.Tools</c> before this
/// middleware runs. It must survive alongside the MCP tools the middleware adds, not be silently
/// dropped by an overwrite.
/// </summary>
public sealed class ToolInjectionMiddlewareTests
{
    [Fact]
    public async Task RunAsync_Should_Merge_TheCallersTools_With_TheProvidersTools()
    {
        // Arrange
        AIFunction mcpTool = AIFunctionFactory.Create(method: () => "catalogue", name: "list_products");
        AIFunction clientTool = AIFunctionFactory.Create(method: () => "card", name: "render_product_card");

        ServiceCollection services = new();
        services.AddScoped<IAgentToolProvider>(implementationFactory: _ => new FixedToolProvider(tools: [mcpTool]));
        await using ServiceProvider provider = services.BuildServiceProvider();

        ToolInjectionMiddleware middleware = new(
            scopeFactory: provider.GetRequiredService<IServiceScopeFactory>(),
            definition: AgentTestFactory.Definition());

        var agent = FakeAgent.Answering(text: "hello");
        ChatClientAgentRunOptions callerOptions = new(chatOptions: new ChatOptions { Tools = [clientTool] });

        // Act
        await middleware.RunAsync(
            messages: [new ChatMessage(role: ChatRole.User, content: "hi")],
            session: null,
            options: callerOptions,
            innerAgent: agent,
            cancellationToken: CancellationToken.None);

        // Assert — the client's tool is still there, alongside the MCP tool the provider supplied.
        ChatClientAgentRunOptions ranWith = agent.LastOptions.ShouldBeOfType<ChatClientAgentRunOptions>();
        ranWith.ChatOptions!.Tools.ShouldNotBeNull();
        ranWith.ChatOptions.Tools.ShouldContain(expected: clientTool);
        ranWith.ChatOptions.Tools.ShouldContain(expected: mcpTool);
    }

    private sealed class FixedToolProvider(IReadOnlyList<AITool> tools) : IAgentToolProvider
    {
        public ValueTask<IReadOnlyList<AITool>> GetToolsAsync(AgentDefinition definition, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(result: tools);
    }
}
