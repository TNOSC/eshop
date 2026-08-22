// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// How an agent's behaviour is realised behind the uniform agent surface.
/// </summary>
/// <remarks>
/// This discriminator is the seam that keeps multi-step orchestration from becoming a rewrite. Both
/// kinds are constructed by the same factory port and are invoked, streamed and exposed identically,
/// so introducing a workflow-backed agent changes one factory and nothing downstream of it.
/// </remarks>
public enum AgentKind
{
    /// <summary>
    /// A single model-backed agent that answers directly, calling tools as it needs them.
    /// </summary>
    Chat = 0,

    /// <summary>
    /// An agent whose behaviour is a multi-step workflow presented as a single agent.
    /// </summary>
    Workflow = 1,
}
