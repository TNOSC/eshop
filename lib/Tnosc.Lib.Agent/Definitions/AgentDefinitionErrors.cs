// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Every way a candidate <see cref="AgentDefinition"/> can break its invariants.
/// </summary>
/// <remarks>
/// Only the definition's own invariants live here. Each constituent value object carries its own
/// error catalogue, so a caller building a definition sees the specific reason a part was rejected.
/// </remarks>
public static class AgentDefinitionErrors
{
    /// <summary>
    /// Gets the error returned when no description was supplied.
    /// </summary>
    /// <remarks>
    /// The description is not decoration: it is what a tool-calling caller reads when deciding
    /// whether to delegate to this agent, so an empty one makes the agent undiscoverable.
    /// </remarks>
    public static Error DescriptionEmpty => Error.Validation(
        code: "AgentDefinition.DescriptionEmpty",
        description: "An agent requires a description.");
}
