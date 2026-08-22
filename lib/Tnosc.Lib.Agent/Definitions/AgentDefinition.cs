// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.ValueObjects;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Everything that distinguishes one agent from another, as validated data.
/// </summary>
/// <remarks>
/// <para>
/// An agent is data, not code. Its identity, its persona, what it is allowed to reach for and how it
/// is asked to answer are all declared here and validated on construction, so a malformed agent
/// fails at startup rather than on a caller's first request.
/// </para>
/// <para>
/// Behaviour deliberately lives elsewhere. Cross-cutting concerns — logging, tool supply, guardrails,
/// approvals — belong in the middleware pipeline wrapped around the agent, never in a branch inside a
/// definition. A definition that started making decisions would be a handler wearing a record's
/// clothes.
/// </para>
/// </remarks>
public sealed record AgentDefinition : ValueObject
{
    private AgentDefinition(
        AgentName name,
        string description,
        AgentKind kind,
        Instructions instructions,
        ToolAllowList tools,
        ModelParameters model,
        AgentOutputContract output)
    {
        Name = name;
        Description = description;
        Kind = kind;
        Instructions = instructions;
        Tools = tools;
        Model = model;
        Output = output;
    }

    /// <summary>
    /// Gets the agent's stable, host-unique name.
    /// </summary>
    public AgentName Name { get; }

    /// <summary>
    /// Gets the human-readable summary of what this agent is for.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets how this agent's behaviour is realised.
    /// </summary>
    public AgentKind Kind { get; }

    /// <summary>
    /// Gets the system prompt defining the agent's role, tone and boundaries.
    /// </summary>
    public Instructions Instructions { get; }

    /// <summary>
    /// Gets the tools this agent is permitted to use.
    /// </summary>
    public ToolAllowList Tools { get; }

    /// <summary>
    /// Gets the portable inference settings for this agent.
    /// </summary>
    public ModelParameters Model { get; }

    /// <summary>
    /// Gets the structured shape this agent is asked to return, if any.
    /// </summary>
    public AgentOutputContract Output { get; }

    /// <summary>
    /// Creates an <see cref="AgentDefinition"/>, validating every part of it.
    /// </summary>
    /// <param name="name">The agent's host-unique name.</param>
    /// <param name="description">A human-readable summary of what the agent is for.</param>
    /// <param name="kind">How the agent's behaviour is realised.</param>
    /// <param name="instructions">The agent's system prompt.</param>
    /// <param name="tools">The tools the agent may use.</param>
    /// <param name="model">The agent's inference settings.</param>
    /// <param name="output">The structured shape the agent must return, or the prose contract.</param>
    /// <returns>
    /// The created <see cref="AgentDefinition"/>, or <c>AgentDefinition.DescriptionEmpty</c> when the
    /// description is blank.
    /// </returns>
    public static Result<AgentDefinition> Create(
        AgentName name,
        string description,
        AgentKind kind,
        Instructions instructions,
        ToolAllowList tools,
        ModelParameters model,
        AgentOutputContract output)
    {
        if (string.IsNullOrWhiteSpace(value: description))
        {
            return AgentDefinitionErrors.DescriptionEmpty;
        }

        return new AgentDefinition(
            name: name,
            description: description,
            kind: kind,
            instructions: instructions,
            tools: tools,
            model: model,
            output: output);
    }
}
