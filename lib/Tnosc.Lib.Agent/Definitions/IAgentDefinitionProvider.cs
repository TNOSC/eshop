// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Supplies one agent definition, so a host can discover its agents by scanning an assembly rather
/// than maintaining a registration list.
/// </summary>
/// <remarks>
/// This marker lives beside <see cref="AgentDefinition"/> rather than with the catalogue that scans
/// for it, because the types that implement it are the application's own agents: putting it next to
/// the catalogue would force every project declaring an agent to depend on the layer that composes
/// them, inverting the dependency direction.
/// </remarks>
public interface IAgentDefinitionProvider
{
    /// <summary>
    /// Gets the agent definition this provider supplies.
    /// </summary>
    AgentDefinition Definition { get; }
}
