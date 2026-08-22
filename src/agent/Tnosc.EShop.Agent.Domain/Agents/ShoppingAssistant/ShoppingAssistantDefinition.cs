// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Agent.Domain.Agents.ShoppingAssistant;

/// <summary>
/// The storefront shopping assistant: answers catalogue questions on a shopper's behalf.
/// </summary>
/// <remarks>
/// <para>
/// The definition is built once in the static constructor and validated there, so a malformed agent
/// fails when the type is first touched at startup rather than on a shopper's first question.
/// </para>
/// <para>
/// The tool allow-list names only reads. That is what keeps this agent safe to expose to any signed-in
/// shopper: even if a prompt talked it into attempting a write, the tool is not in its hands to call.
/// The tool server's own permission checks remain the authority — this list is defence in depth, not a
/// replacement for them.
/// </para>
/// </remarks>
internal sealed class ShoppingAssistantDefinition : IAgentDefinitionProvider
{
    /// <summary>
    /// The catalogue tools this agent may use. Reads only, deliberately.
    /// </summary>
    /// <remarks>
    /// Named through <see cref="McpToolNames"/> rather than as a literal — <c>Mcp.Tool</c> declares
    /// the identical constant on the tool's <c>[McpServerTool(Name = ...)]</c>, so the two sides
    /// cannot drift apart the way an independently spelled string could.
    /// </remarks>
    private static readonly string[] PermittedTools = [McpToolNames.ListProducts];

    static ShoppingAssistantDefinition() => Definition = Build();

    /// <summary>
    /// Gets the validated definition of the shopping assistant.
    /// </summary>
    public static AgentDefinition Definition { get; }

    /// <inheritdoc />
    AgentDefinition IAgentDefinitionProvider.Definition => Definition;

    private static AgentDefinition Build()
    {
        Result<AgentName> name = AgentName.Create(value: AgentNames.ShoppingAssistant);
        Result<Instructions> instructions = Instructions.Create(value: ShoppingAssistantInstructions.Text);
        Result<ToolAllowList> tools = ToolAllowList.Create(names: PermittedTools);
        Result<ModelParameters> model = ModelParameters.Create(temperature: 0.2f, maxOutputTokens: 1_024);

        Result<AgentDefinition> definition = AgentDefinition.Create(
            name: name.Value,
            description: "Answers questions about the product catalogue for a shopper.",
            kind: AgentKind.Chat,
            instructions: instructions.Value,
            tools: tools.Value,
            model: model.Value,
            output: AgentOutputContract.None);

        return definition.Value;
    }
}
