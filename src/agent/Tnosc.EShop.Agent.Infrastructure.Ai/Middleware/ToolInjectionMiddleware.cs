// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Runtime;

namespace Tnosc.EShop.Agent.Infrastructure.Ai.Middleware;

/// <summary>
/// Supplies an agent's tools freshly on every run, rather than once when the agent is built.
/// </summary>
/// <remarks>
/// <para>
/// Tools are resolved per run because discovering them happens under the calling user's own
/// identity, so a set resolved once and shared would hand every caller the first caller's reach.
/// The framework's own way of attaching tools to a hosted agent cannot express that: it resolves
/// tools from the container at registration time and rejects a shorter-lived tool than the agent it
/// belongs to, and its factory is synchronous while discovery is not.
/// </para>
/// <para>
/// A discovery failure is deliberately allowed to propagate. Falling back to an empty tool set would
/// leave the agent answering catalogue questions from whatever it remembers, which reads to a user
/// as confident invention rather than as the outage it actually is.
/// </para>
/// <para>
/// This middleware — and therefore the <see cref="AIAgent"/> it wraps — is built once and shared as a
/// singleton: AG-UI's <c>MapAGUIServer(agentName, pattern)</c> resolves the named agent from the
/// container's <em>root</em> provider at endpoint-mapping time, not per request, so a scoped agent
/// throws <c>Cannot resolve scoped service ... from root provider</c> the moment a request arrives.
/// <see cref="IAgentToolProvider"/> stays scoped — it carries the caller's forwarded token — so a
/// fresh <see cref="IServiceScope"/> is opened per run instead, and kept alive for the run's entire
/// duration, because a tool obtained from it may be invoked mid-conversation.
/// </para>
/// </remarks>
internal sealed class ToolInjectionMiddleware(IServiceScopeFactory scopeFactory, AgentDefinition definition)
{
    /// <summary>
    /// Runs the agent with this run's tools attached.
    /// </summary>
    /// <param name="messages">The conversation being sent.</param>
    /// <param name="session">The conversation state.</param>
    /// <param name="options">The run options supplied by the caller, if any.</param>
    /// <param name="innerAgent">The agent being wrapped.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The agent's response.</returns>
    public async Task<AgentResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        AgentRunOptions withTools = await WithToolsAsync(
            toolProvider: scope.ServiceProvider.GetRequiredService<IAgentToolProvider>(),
            options: options,
            cancellationToken: cancellationToken);

        return await innerAgent.RunAsync(
            messages: messages,
            session: session,
            options: withTools,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Streams the agent's answer with this run's tools attached.
    /// </summary>
    /// <param name="messages">The conversation being sent.</param>
    /// <param name="session">The conversation state.</param>
    /// <param name="options">The run options supplied by the caller, if any.</param>
    /// <param name="innerAgent">The agent being wrapped.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The answer's updates, in order.</returns>
    public async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        AgentRunOptions withTools = await WithToolsAsync(
            toolProvider: scope.ServiceProvider.GetRequiredService<IAgentToolProvider>(),
            options: options,
            cancellationToken: cancellationToken);

        IAsyncEnumerable<AgentResponseUpdate> updates = innerAgent.RunStreamingAsync(
            messages: messages,
            session: session,
            options: withTools,
            cancellationToken: cancellationToken);

        await foreach (AgentResponseUpdate update in updates.WithCancellation(cancellationToken: cancellationToken))
        {
            yield return update;
        }
    }

    private async ValueTask<AgentRunOptions> WithToolsAsync(
        IAgentToolProvider toolProvider,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AITool> tools = await toolProvider.GetToolsAsync(
            definition: definition,
            cancellationToken: cancellationToken);

        // Start from whatever the caller asked for so nothing they set is dropped, then add the
        // tools. A caller-supplied ChatOptions instance is cloned rather than mutated: it may be
        // reused across runs, and quietly accumulating tools on it would leak one run into the next.
        ChatOptions chatOptions = options is ChatClientAgentRunOptions { ChatOptions: not null } chatRunOptions
            ? chatRunOptions.ChatOptions.Clone()
            : new ChatOptions();

        // A caller (AG-UI's own client-declared-tool mechanism) may already have put tools on
        // chatOptions.Tools before this middleware runs — a render-only frontend tool the client
        // will handle itself, never executed here. Merge rather than overwrite, or that tool
        // silently disappears from the run.
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. tools];

        return new ChatClientAgentRunOptions(chatOptions: chatOptions);
    }
}
