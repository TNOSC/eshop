// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tnosc.EShop.Agent.Infrastructure.Ai.Middleware;
using Tnosc.EShop.Agent.Infrastructure.Ai.Options;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Runtime;

namespace Tnosc.EShop.Agent.Infrastructure.Ai;

/// <summary>
/// Builds a runnable agent from its definition, backed by a Microsoft Foundry model.
/// </summary>
/// <remarks>
/// <para>
/// This is where a definition — inert, validated data — becomes something that can answer. It is
/// also the only place that names a model provider, which is what keeps the rest of the solution
/// portable.
/// </para>
/// <para>
/// The pipeline wrapped around every agent is assembled here, outermost last, in the same spirit as
/// the decorator chain this solution already wraps around its command handlers. Telemetry and
/// logging come from the framework rather than being hand-rolled; only the per-run tool supply,
/// which the framework cannot express, is custom.
/// </para>
/// <para>
/// Built and registered as a singleton — see <see cref="Middleware.ToolInjectionMiddleware"/> for why
/// the scoped <see cref="IAgentToolProvider"/> is resolved per run through an
/// <see cref="IServiceScopeFactory"/> rather than injected here directly.
/// </para>
/// </remarks>
internal sealed class ChatClientAgentFactory(
    AIProjectClient projectClient,
    FoundryOptions options,
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory) : IAgentFactory
{
    /// <summary>
    /// The activity source name agent telemetry is emitted under.
    /// </summary>
    public const string ActivitySourceName = "Tnosc.EShop.Agent";

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// Thrown for an agent kind this factory does not back. Multi-step workflow agents are satisfied
    /// by a separate factory over the same contract, so adding them changes this class and nothing
    /// downstream of it.
    /// </exception>
    public AIAgent Create(AgentDefinition definition)
    {
        if (definition.Kind is not AgentKind.Chat)
        {
            throw new NotSupportedException(
                message: $"Agent '{definition.Name.Value}' is of kind {definition.Kind}, which this factory does not build.");
        }

        ChatClientAgent agent = projectClient.AsAIAgent(
            options: BuildAgentOptions(definition: definition),
            loggerFactory: loggerFactory);

        ToolInjectionMiddleware toolInjection = new(scopeFactory: scopeFactory, definition: definition);

        return agent
            .AsBuilder()
            .Use(
                runFunc: toolInjection.RunAsync,
                runStreamingFunc: toolInjection.RunStreamingAsync)
            .UseOpenTelemetry(sourceName: ActivitySourceName)
            .UseLogging(loggerFactory: loggerFactory)
            .Build();
    }

    private ChatClientAgentOptions BuildAgentOptions(AgentDefinition definition)
    {
        ChatOptions chatOptions = new()
        {
            ModelId = options.DeploymentName,
            Instructions = definition.Instructions.Value,
            Temperature = definition.Model.Temperature,
            MaxOutputTokens = definition.Model.MaxOutputTokens,
        };

        if (definition.Output is { IsDeclared: true, OutputType: not null })
        {
            // Applied to the agent rather than to an individual run: declaring an output shape is a
            // property of what the agent *is*, so every way of invoking it should get the same shape
            // back rather than only the strongly-typed overload.
            chatOptions.ResponseFormat = ChatResponseFormat.ForJsonSchema(
                schemaType: definition.Output.OutputType,
                serializerOptions: AIJsonUtilities.DefaultOptions,
                schemaName: definition.Output.OutputType.Name,
                schemaDescription: $"The answer shape required of agent '{definition.Name.Value}'.");
        }

        return new ChatClientAgentOptions
        {
            Name = definition.Name.Value,
            Description = definition.Description,
            ChatOptions = chatOptions,
        };
    }
}
