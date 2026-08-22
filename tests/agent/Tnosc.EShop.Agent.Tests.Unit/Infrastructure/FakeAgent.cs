// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Tnosc.EShop.Agent.Tests.Unit.Infrastructure;

/// <summary>
/// An agent that answers with whatever a test tells it to, or throws.
/// </summary>
/// <remarks>
/// Hand-written rather than substituted: an agent returns streamed updates and carries an abstract
/// session, and a mock's setup for that is harder to read than the twenty lines it replaces.
/// </remarks>
internal sealed class FakeAgent : AIAgent
{
    private readonly AgentResponse? _response;
    private readonly Exception? _failure;

    private FakeAgent(AgentResponse? response, Exception? failure)
    {
        _response = response;
        _failure = failure;
    }

    /// <summary>
    /// Creates an agent that answers with the given text and messages.
    /// </summary>
    /// <param name="text">The answer text.</param>
    /// <param name="messages">The messages the run produced, carrying any tool calls.</param>
    /// <param name="usage">What the run reported consuming.</param>
    /// <returns>The fake agent.</returns>
    public static FakeAgent Answering(
        string text,
        IList<ChatMessage>? messages = null,
        UsageDetails? usage = null)
    {
        // The answer text is carried by an assistant message, exactly as a real provider returns it —
        // the response's Text is derived from its messages rather than set independently. Any tool
        // call and result a test supplies comes first, so the ordering matches a real run.
        List<ChatMessage> all = [.. messages ?? []];
        all.Add(item: new ChatMessage(role: ChatRole.Assistant, content: text));

        return new FakeAgent(
            response: new AgentResponse(messages: all)
            {
                ResponseId = "run-1",
                Usage = usage,
            },
            failure: null);
    }

    /// <summary>
    /// Creates an agent whose every run throws.
    /// </summary>
    /// <param name="failure">The exception to throw.</param>
    /// <returns>The fake agent.</returns>
    public static FakeAgent Failing(Exception failure) => new(response: null, failure: failure);

    /// <summary>
    /// Gets the options the most recent run was given, so a test can assert on what a middleware
    /// attached to it.
    /// </summary>
    public AgentRunOptions? LastOptions { get; private set; }

    /// <inheritdoc />
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(result: new FakeSession());

    /// <inheritdoc />
    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastOptions = options;

        if (_failure is not null)
        {
            throw _failure;
        }

        return Task.FromResult(result: _response!);
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastOptions = options;

        if (_failure is not null)
        {
            throw _failure;
        }

        await Task.Yield();

        yield return new AgentResponseUpdate(role: ChatRole.Assistant, content: _response!.Text);
    }

    /// <inheritdoc />
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(result: default(JsonElement));

    /// <inheritdoc />
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<AgentSession>(result: new FakeSession());

    private sealed class FakeSession : AgentSession;
}
