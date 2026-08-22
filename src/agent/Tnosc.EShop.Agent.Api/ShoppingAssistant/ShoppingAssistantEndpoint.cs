// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Tnosc.EShop.Agent.Domain.Agents;
using Tnosc.Lib.Api.Abstractions;

namespace Tnosc.EShop.Agent.Api.ShoppingAssistant;

/// <summary>
/// Exposes the shopping assistant over the AG-UI protocol as a streaming conversation endpoint.
/// </summary>
internal sealed class ShoppingAssistantEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        // Both parameters are strings and the framework takes the agent name first. Naming them is
        // what keeps that from silently inverting into a route called "shopping-assistant" serving an
        // agent called "/agents/shopping-assistant" — which compiles, starts, and 404s.
        app.MapAGUIServer(
               agentName: AgentNames.ShoppingAssistant,
               pattern: AgentRoutes.ShoppingAssistant)
           .WithTags(AgentRoutes.Tag)
           .WithName("ShoppingAssistant")
           .WithSummary("Converse with the shopping assistant.")
           .WithDescription(
               """
               Streams the assistant's answer as AG-UI server-sent events. Send an AG-UI RunAgentInput
               body and accept text/event-stream.

               Any authenticated caller may talk to the assistant. Authorization is not enforced here
               on purpose: the assistant reaches the catalogue through tools that check the caller's
               own token, so a shopper gets exactly the reach they already had. A tool refusing them
               is reported back as part of the conversation rather than failing the request.

               The threadId in the request body resumes a conversation. It is a continuation
               identifier, not a credential — conversations are isolated per authenticated caller, so
               presenting someone else's threadId does not reach their conversation.
               """)
           .RequireAuthorization();
}
