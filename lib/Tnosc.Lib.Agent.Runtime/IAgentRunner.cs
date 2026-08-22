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
using Tnosc.Lib.Agent.Results;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Runtime;

/// <summary>
/// Invokes a named agent and returns its outcome as an ordinary result.
/// </summary>
/// <remarks>
/// <para>
/// This is the in-process contract for talking to an agent, and the reason it lives in a reusable
/// library rather than beside its implementation. Any caller — an application service, a background
/// job, or another agent delegating to this one — can invoke an agent through this interface without
/// depending on which model provider serves it, where its tools come from, or how it is exposed over
/// the wire.
/// </para>
/// <para>
/// A protocol endpoint is therefore a <em>presentation</em> of this contract, not the only way in,
/// which is the same relationship a result-to-HTTP mapping has with the result it maps.
/// </para>
/// </remarks>
public interface IAgentRunner
{
    /// <summary>
    /// Runs an agent and waits for its complete answer.
    /// </summary>
    /// <param name="agentName">The name of the agent to run.</param>
    /// <param name="messages">The conversation to send, newest last.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>
    /// The agent's outcome, or an <see cref="AgentErrors"/> failure when the run could not happen or
    /// could not finish.
    /// </returns>
    ValueTask<Result<AgentResult>> RunAsync(
        string agentName,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an agent and binds its answer to the shape its definition declared.
    /// </summary>
    /// <typeparam name="TOutput">The shape the answer must bind to.</typeparam>
    /// <param name="agentName">The name of the agent to run.</param>
    /// <param name="messages">The conversation to send, newest last.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>
    /// The agent's outcome with its bound answer, or an <see cref="AgentErrors"/> failure — including
    /// <c>Agent.OutputBindingFailed</c> when the answer did not fit <typeparamref name="TOutput"/>.
    /// </returns>
    ValueTask<Result<AgentResult<TOutput>>> RunAsync<TOutput>(
        string agentName,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an agent and streams its answer as it is produced.
    /// </summary>
    /// <param name="agentName">The name of the agent to run.</param>
    /// <param name="messages">The conversation to send, newest last.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The answer's updates, in order.</returns>
    /// <remarks>
    /// This overload returns updates rather than a result because a stream has already begun
    /// succeeding by the time most failures occur; a caller consuming it observes an error as a
    /// terminal update, not as a return value.
    /// </remarks>
    IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        string agentName,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
