// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Security.Claims;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.EShop.Agent.Application.Catalog;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Runtime;

namespace Tnosc.EShop.Agent.Host.Extensions;

/// <summary>
/// Registers every agent in the catalogue as a named agent the protocol endpoints can resolve.
/// </summary>
internal static class HostedAgentExtensions
{
    /// <summary>
    /// Registers each catalogued agent under its own name, with an isolated conversation store.
    /// </summary>
    /// <param name="builder">The host application builder to register agents on.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Conversation isolation is a security requirement here, not a refinement.</strong> The
    /// AG-UI thread identifier arrives from the wire and is treated by the protocol as a
    /// chain-resume identifier, never as a credential — the session store contract carries no notion
    /// of who owns a conversation. Left alone, that means any caller who knows or guesses another
    /// caller's thread identifier resumes <em>their</em> conversation, reading back whatever it
    /// contained. This host serves many users behind Keycloak, so it composes the caller's identity
    /// into the storage key.
    /// </para>
    /// <para>
    /// Two details make that work, and both are easy to undo by accident. The isolation provider has
    /// to be registered — see <c>Program.cs</c> — and the session store has to be attached
    /// <em>through the agent builder</em> below, because only that path wraps the store in the
    /// isolating decorator. Registering a session store directly into the container instead looks
    /// equivalent, starts cleanly, and silently reopens the leak.
    /// </para>
    /// <para>
    /// The store is in-memory for now, which means conversations are lost on restart. That is a
    /// deliberate first step: the store is the seam, so moving conversations to durable storage later
    /// replaces this one call and nothing else — and the isolation wrapper keeps applying.
    /// </para>
    /// </remarks>
    internal static IHostApplicationBuilder AddHostedAgents(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        // The catalogue is a singleton over compiled-in definitions, so building a provider here to
        // enumerate it costs nothing and avoids duplicating the discovery that already happened.
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IAgentCatalog catalog = provider.GetRequiredService<IAgentCatalog>();

        foreach (AgentDefinition definition in catalog.All)
        {
            AgentDefinition captured = definition;

            // Singleton, not scoped: MapAGUIServer(agentName, pattern) resolves the named agent as a
            // keyed service from the root provider once, at endpoint-mapping time — a scoped
            // registration throws "Cannot resolve scoped service ... from root provider" on the first
            // request. IAgentFactory builds an agent whose tool supply is resolved per run from a
            // fresh DI scope instead, so the built AIAgent itself carries no per-caller state.
            builder.Services
                .AddAIAgent(
                    name: captured.Name.Value,
                    createAgentDelegate: (serviceProvider, _) => serviceProvider
                        .GetRequiredService<IAgentFactory>()
                        .Create(definition: captured),
                    lifetime: ServiceLifetime.Singleton)
                .WithInMemorySessionStore();
        }

        return builder;
    }

    /// <summary>
    /// The claim the caller's conversations are isolated by.
    /// </summary>
    /// <remarks>
    /// The subject claim, because it is the one identifier Keycloak guarantees is stable and unique
    /// for a user. An email or a username can be reassigned, which would hand a new account the
    /// previous holder's conversations.
    /// </remarks>
    internal const string IsolationClaimType = ClaimTypes.NameIdentifier;
}
