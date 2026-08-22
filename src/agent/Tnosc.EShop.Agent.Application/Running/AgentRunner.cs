// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Tnosc.EShop.Agent.Application.Catalog;
using Tnosc.Lib.Agent;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Results;
using Tnosc.Lib.Agent.Runtime;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Agent.Application.Running;

/// <summary>
/// Invokes agents by name and projects their outcome onto <see cref="AgentResult"/>.
/// </summary>
/// <remarks>
/// This is the only place that decides what counts as a failed run. Everything that prevented the
/// agent from answering becomes an error; everything the agent said, including that it could not
/// help or that a tool refused it, is content. See <see cref="AgentErrors"/>.
/// </remarks>
internal sealed class AgentRunner(
    IAgentCatalog catalog,
    IAgentFactory factory,
    ILogger<AgentRunner> logger) : IAgentRunner
{
    /// <inheritdoc />
    public async ValueTask<Result<AgentResult>> RunAsync(
        string agentName,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (!catalog.TryGet(agentName: agentName, definition: out AgentDefinition? definition))
        {
            return AgentErrors.NotFound(agentName: agentName);
        }

        AIAgent agent = factory.Create(definition: definition);

        try
        {
            AgentSession session = await agent.CreateSessionAsync(cancellationToken: cancellationToken);

            AgentResponse response = await agent.RunAsync(
                messages: messages,
                session: session,
                options: null,
                cancellationToken: cancellationToken);

            // Text is absent when a run produced only tool traffic and no prose. That is a normal
            // outcome, so it becomes an empty answer rather than a null a caller has to guard.
            return new AgentResult(
                Text: response.Text ?? string.Empty,
                Metadata: AgentResponseMapper.ToMetadata(
                    agentName: agentName,
                    responseId: response.ResponseId,
                    threadId: null,
                    messages: response.Messages,
                    usage: response.Usage));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception: exception,
                message: "Agent {AgentName} failed to complete its run",
                args: agentName);
            return AgentErrors.RunFailed(agentName: agentName);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Result<AgentResult<TOutput>>> RunAsync<TOutput>(
        string agentName,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (!catalog.TryGet(agentName: agentName, definition: out AgentDefinition? definition))
        {
            return AgentErrors.NotFound(agentName: agentName);
        }

        AIAgent agent = factory.Create(definition: definition);

        AgentResponse<TOutput> response;

        try
        {
            AgentSession session = await agent.CreateSessionAsync(cancellationToken: cancellationToken);

            response = await agent.RunAsync<TOutput>(
                messages: messages,
                session: session,
                serializerOptions: null,
                options: null,
                cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception: exception,
                message: "Agent {AgentName} failed to complete its run",
                args: agentName);
            return AgentErrors.RunFailed(agentName: agentName);
        }

        AgentRunMetadata metadata = AgentResponseMapper.ToMetadata(
            agentName: agentName,
            responseId: response.ResponseId,
            threadId: null,
            messages: response.Messages,
            usage: response.Usage);

        // Reading Result is what actually performs the binding, so it is deliberately outside the
        // catch above: a binding failure is a different fault from a run failure, and collapsing the
        // two would report an unreachable model when the real problem is a mismatched schema.
        TOutput bound;

        try
        {
            bound = response.Result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception: exception,
                message: "Agent {AgentName} produced an answer that did not match its declared output shape",
                args: agentName);
            return AgentErrors.OutputBindingFailed(agentName: agentName);
        }

        return new AgentResult<TOutput>(
            Text: response.Text ?? string.Empty,
            Output: bound,
            Metadata: metadata);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        string agentName,
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!catalog.TryGet(agentName: agentName, definition: out AgentDefinition? definition))
        {
            // A stream has no error channel of its own, and the caller is already iterating by the
            // time this runs. Throwing is the only way to say "no such agent" that cannot be mistaken
            // for the agent having simply had nothing to say.
            throw new InvalidOperationException(message: $"No agent named '{agentName}' is registered.");
        }

        AIAgent agent = factory.Create(definition: definition);
        AgentSession session = await agent.CreateSessionAsync(cancellationToken: cancellationToken);

        IAsyncEnumerable<AgentResponseUpdate> updates = agent.RunStreamingAsync(
            messages: messages,
            session: session,
            options: null,
            cancellationToken: cancellationToken);

        await foreach (AgentResponseUpdate update in updates.WithCancellation(cancellationToken: cancellationToken))
        {
            yield return update;
        }
    }
}
