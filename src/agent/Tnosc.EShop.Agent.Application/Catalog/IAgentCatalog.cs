// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tnosc.Lib.Agent.Definitions;

namespace Tnosc.EShop.Agent.Application.Catalog;

/// <summary>
/// The set of agents this host serves, resolvable by name.
/// </summary>
/// <remarks>
/// This contract stays in the application rather than moving to the reusable agent library, and the
/// distinction is worth keeping straight. The library holds the seams a different host would fill in
/// differently — which model provider runs an agent, where its tools come from, how it is invoked.
/// A catalogue is not one of those: every host discovers its own agents the same way, so there is
/// exactly one implementation and it is consumed inside this same assembly. It remains an interface
/// only so the runner can be tested against a stub, not because a second implementation is expected.
/// </remarks>
public interface IAgentCatalog
{
    /// <summary>
    /// Gets every agent this host serves.
    /// </summary>
    IReadOnlyCollection<AgentDefinition> All { get; }

    /// <summary>
    /// Finds an agent by name.
    /// </summary>
    /// <param name="agentName">The agent's name.</param>
    /// <param name="definition">The agent found, when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when an agent of that name is registered.</returns>
    bool TryGet(string agentName, [NotNullWhen(returnValue: true)] out AgentDefinition? definition);
}
