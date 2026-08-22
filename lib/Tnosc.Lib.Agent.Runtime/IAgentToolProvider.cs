// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Tnosc.Lib.Agent.Definitions;

namespace Tnosc.Lib.Agent.Runtime;

/// <summary>
/// Supplies the tools an agent may use for a given run, already filtered by its allow-list.
/// </summary>
/// <remarks>
/// <para>
/// Asynchronous and per-run by necessity, not by preference. Tools are typically discovered from a
/// remote server rather than declared in code, and where that server enforces the caller's own
/// permissions the discovery has to happen under the caller's identity — which rules out resolving a
/// single tool set once at startup and sharing it across everyone.
/// </para>
/// <para>
/// An implementation must let a discovery failure surface. Returning an empty tool set on error
/// produces an agent that answers confidently from memory instead of from data, which reads as a
/// hallucination rather than as the outage it is.
/// </para>
/// </remarks>
public interface IAgentToolProvider
{
    /// <summary>
    /// Gets the tools this agent may use, filtered by <see cref="AgentDefinition.Tools"/>.
    /// </summary>
    /// <param name="definition">The agent whose tools are being resolved.</param>
    /// <param name="cancellationToken">Cancels the discovery call.</param>
    /// <returns>The permitted tools. Empty when the server exposes none the agent may use.</returns>
    ValueTask<IReadOnlyList<AITool>> GetToolsAsync(
        AgentDefinition definition,
        CancellationToken cancellationToken = default);
}
