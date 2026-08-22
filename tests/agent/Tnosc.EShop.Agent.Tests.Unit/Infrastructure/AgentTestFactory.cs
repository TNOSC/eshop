// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using Tnosc.Lib.Agent.Definitions;

namespace Tnosc.EShop.Agent.Tests.Unit.Infrastructure;

/// <summary>
/// Builds valid agent definitions so a test can state only the part it cares about.
/// </summary>
internal static class AgentTestFactory
{
    /// <summary>
    /// Creates a valid definition, overriding only what a test names.
    /// </summary>
    /// <param name="name">The agent name. Defaults to a valid one.</param>
    /// <param name="tools">The tool allow-list. Defaults to unrestricted.</param>
    /// <param name="kind">The agent kind. Defaults to a chat agent.</param>
    /// <param name="output">The output contract. Defaults to prose.</param>
    /// <returns>A valid <see cref="AgentDefinition"/>.</returns>
    public static AgentDefinition Definition(
        string name = "test-agent",
        IEnumerable<string>? tools = null,
        AgentKind kind = AgentKind.Chat,
        AgentOutputContract? output = null) =>
        AgentDefinition.Create(
            name: AgentName.Create(value: name).Value,
            description: "An agent used by tests.",
            kind: kind,
            instructions: Instructions.Create(value: "Be helpful.").Value,
            tools: ToolAllowList.Create(names: tools).Value,
            model: ModelParameters.Default,
            output: output ?? AgentOutputContract.None).Value;
}
