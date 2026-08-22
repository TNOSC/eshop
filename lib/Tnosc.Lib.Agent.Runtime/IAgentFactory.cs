// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Agents.AI;
using Tnosc.Lib.Agent.Definitions;

namespace Tnosc.Lib.Agent.Runtime;

/// <summary>
/// Turns a validated agent definition into a runnable agent.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam between what an agent <em>is</em> and what actually runs it. An implementation
/// chooses the model provider, applies the definition's instructions and inference settings, and
/// wraps the result in whatever middleware the host wants around every run.
/// </para>
/// <para>
/// It is also where multi-step orchestration will arrive. A second implementation selected by
/// <see cref="AgentDefinition.Kind"/> can back an agent with a workflow instead of a single model
/// call, and because the return type is unchanged, nothing that invokes or exposes agents has to
/// know the difference.
/// </para>
/// </remarks>
public interface IAgentFactory
{
    /// <summary>
    /// Creates a runnable agent from its definition.
    /// </summary>
    /// <param name="definition">The agent to create.</param>
    /// <returns>The runnable agent.</returns>
    AIAgent Create(AgentDefinition definition);
}
