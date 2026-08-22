// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Tnosc.EShop.Agent.Application.Catalog;
using Tnosc.EShop.Agent.Application.Running;
using Tnosc.EShop.Agent.Tests.Unit.Infrastructure;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Results;
using Tnosc.Lib.Agent.Runtime;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Running;

/// <summary>
/// The runner decides what counts as a failed run — everything else the agent said is content.
/// </summary>
public sealed class AgentRunnerTests
{
    [Fact]
    public async Task RunAsync_Should_Fail_When_TheAgentIsUnknown()
    {
        // Arrange
        AgentRunner runner = RunnerFor(agent: FakeAgent.Answering(text: "hello"));

        // Act
        Result<AgentResult> result = await runner.RunAsync(
            agentName: "no-such-agent",
            messages: [new ChatMessage(role: ChatRole.User, content: "hi")]);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Agent.NotFound");
    }

    [Fact]
    public async Task RunAsync_Should_Fail_When_TheRunThrows()
    {
        // A provider that is unreachable, throttling or rejecting credentials all arrive here as an
        // exception, and all of them mean the same thing to a caller: no answer was produced.

        // Arrange
        AgentRunner runner = RunnerFor(
            agent: FakeAgent.Failing(failure: new HttpRequestException(message: "unreachable")));

        // Act
        Result<AgentResult> result = await runner.RunAsync(
            agentName: "test-agent",
            messages: [new ChatMessage(role: ChatRole.User, content: "hi")]);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Agent.RunFailed");
    }

    [Fact]
    public async Task RunAsync_Should_Report_TheToolsTheAgentCalled()
    {
        // This is what makes a run assertable without reading the model's prose, which is the
        // difference between a stable behavioural test and one that breaks on a reworded answer.

        // Arrange
        AgentRunner runner = RunnerFor(agent: FakeAgent.Answering(
            text: "Here are the products.",
            messages: MessagesWithToolCall(toolName: "ListProducts", failure: null)));

        // Act
        Result<AgentResult> result = await runner.RunAsync(
            agentName: "test-agent",
            messages: [new ChatMessage(role: ChatRole.User, content: "what do you have?")]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Metadata.ToolCalls.ShouldHaveSingleItem().Name.ShouldBe(expected: "ListProducts");
        result.Value.Metadata.ToolCalls[0].Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_Should_Treat_ARefusedTool_As_Content_Not_AsAnError()
    {
        // A tool refusing a caller who lacks permission is the system working correctly. Reporting it
        // as a failed run would turn a correctly enforced permission into a 500, and would stop the
        // agent from telling the user what actually happened.

        // Arrange
        AgentRunner runner = RunnerFor(agent: FakeAgent.Answering(
            text: "You are not allowed to do that.",
            messages: MessagesWithToolCall(
                toolName: "CreateProduct",
                failure: new UnauthorizedAccessException(message: "forbidden"))));

        // Act
        Result<AgentResult> result = await runner.RunAsync(
            agentName: "test-agent",
            messages: [new ChatMessage(role: ChatRole.User, content: "add a product")]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Metadata.ToolCalls.ShouldHaveSingleItem().Succeeded.ShouldBeFalse();
        result.Value.Text.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RunAsync_Should_Carry_ReportedUsage()
    {
        // Arrange
        AgentRunner runner = RunnerFor(agent: FakeAgent.Answering(
            text: "hi",
            usage: new UsageDetails { InputTokenCount = 12, OutputTokenCount = 34 }));

        // Act
        Result<AgentResult> result = await runner.RunAsync(
            agentName: "test-agent",
            messages: [new ChatMessage(role: ChatRole.User, content: "hi")]);

        // Assert
        result.Value.Metadata.Usage.InputTokens.ShouldBe(expected: 12);
        result.Value.Metadata.Usage.OutputTokens.ShouldBe(expected: 34);
    }

    [Fact]
    public async Task RunAsync_Should_Report_UnknownUsage_When_TheProviderReportsNone()
    {
        // Absence is honest here; a zero would claim the run was free.

        // Arrange
        AgentRunner runner = RunnerFor(agent: FakeAgent.Answering(text: "hi", usage: null));

        // Act
        Result<AgentResult> result = await runner.RunAsync(
            agentName: "test-agent",
            messages: [new ChatMessage(role: ChatRole.User, content: "hi")]);

        // Assert
        result.Value.Metadata.Usage.InputTokens.ShouldBeNull();
        result.Value.Metadata.Usage.OutputTokens.ShouldBeNull();
    }

    [Fact]
    public async Task RunStreamingAsync_Should_Throw_When_TheAgentIsUnknown()
    {
        // A stream has no error channel, and the caller is already iterating. Throwing is the only
        // signal that cannot be mistaken for the agent simply having had nothing to say.

        // Arrange
        AgentRunner runner = RunnerFor(agent: FakeAgent.Answering(text: "hi"));

        // Act
        async Task Consume()
        {
            await foreach (AgentResponseUpdate _ in runner.RunStreamingAsync(
                agentName: "no-such-agent",
                messages: [new ChatMessage(role: ChatRole.User, content: "hi")]))
            {
                // Enumerating is the act under test.
            }
        }

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(actual: Consume);
    }

    private static List<ChatMessage> MessagesWithToolCall(string toolName, Exception? failure)
    {
        FunctionCallContent call = new(callId: "call-1", name: toolName, arguments: null);
        FunctionResultContent result = new(callId: "call-1", result: "{}") { Exception = failure };

        return
        [
            new ChatMessage(role: ChatRole.Assistant, contents: [call]),
            new ChatMessage(role: ChatRole.Tool, contents: [result]),
        ];
    }

    private static AgentRunner RunnerFor(AIAgent agent) =>
        new(catalog: new SingleAgentCatalog(only: AgentTestFactory.Definition()),
            factory: new StubFactory(agent: agent),
            logger: NullLogger<AgentRunner>.Instance);

    private sealed class StubFactory(AIAgent agent) : IAgentFactory
    {
        public AIAgent Create(AgentDefinition definition) => agent;
    }

    private sealed class SingleAgentCatalog(AgentDefinition only) : IAgentCatalog
    {
        public IReadOnlyCollection<AgentDefinition> All => [only];

        public bool TryGet(string agentName, [NotNullWhen(returnValue: true)] out AgentDefinition? definition)
        {
            definition = string.Equals(a: agentName, b: only.Name.Value, comparisonType: StringComparison.Ordinal)
                ? only
                : null;

            return definition is not null;
        }
    }
}
