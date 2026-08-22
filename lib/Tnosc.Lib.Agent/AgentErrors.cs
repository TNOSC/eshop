// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent;

/// <summary>
/// Every way an agent run can fail to happen or fail to finish.
/// </summary>
/// <remarks>
/// These are the <em>enclosing</em> failures only. An agent that ran and answered is a success even
/// when a tool refused it or it declined to help — those are content. See the remarks on
/// <see cref="Results.AgentResult"/> for why the line is drawn there.
/// </remarks>
public static class AgentErrors
{
    /// <summary>
    /// Gets the error returned when no agent is registered under the requested name.
    /// </summary>
    public static Error NotFound(string agentName) => Error.NotFound(
        code: "Agent.NotFound",
        description: $"No agent named '{agentName}' is registered.");

    /// <summary>
    /// Gets the error returned when the run could not be completed.
    /// </summary>
    /// <remarks>
    /// Covers everything between dispatching the run and receiving an answer: the provider being
    /// unreachable, rejecting the credentials, throttling, or the run being cancelled.
    /// </remarks>
    public static Error RunFailed(string agentName) => Error.Failure(
        code: "Agent.RunFailed",
        description: $"The agent '{agentName}' could not complete the run.");

    /// <summary>
    /// Gets the error returned when the agent's answer would not bind to its declared output shape.
    /// </summary>
    /// <remarks>
    /// Deliberately a failure rather than a success carrying a null. The caller asked for a shape;
    /// handing back an empty one and letting them discover it later hides the fault at the point it
    /// is cheapest to see.
    /// </remarks>
    public static Error OutputBindingFailed(string agentName) => Error.Failure(
        code: "Agent.OutputBindingFailed",
        description: $"The answer from agent '{agentName}' did not match its declared output shape.");
}
