// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AGUI.Client;
using Microsoft.Extensions.AI;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Infrastructure;
using Tnosc.EShop.Client.Web.Contracts.Routes;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// Talks to the agent host over AG-UI. This is the only type in the client that knows the protocol
/// exists, the same role <see cref="CatalogApi"/> plays for the API's HTTP+JSON endpoints.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AGUIChatClient"/> is an <see cref="IChatClient"/> over AG-UI's server-sent-event
/// transport, so nothing here parses events, correlates message ids or builds a run input by hand.
/// It is constructed per call rather than held as a field: it is disposable, it does not own the
/// <see cref="HttpClient"/> it is handed, and constructing one costs nothing next to a model round
/// trip.
/// </para>
/// <para>
/// The route is relative on purpose. The transport posts to it against
/// <see cref="HttpClient.BaseAddress"/>, which is the BFF's <c>/bff/</c> prefix under WebAssembly and
/// the agent host itself under interactive Server — the same arrangement every other typed client
/// here uses.
/// </para>
/// </remarks>
internal sealed class ShoppingAssistantApi(HttpClient httpClient) : IShoppingAssistantApi
{
    /// <summary>
    /// The client-declared, render-only tool the agent may call to have a product shown as a card.
    /// It has no invoke delegate, so <see cref="AGUIChatClient"/> never executes it — the call
    /// surfaces to the caller as a <see cref="FunctionCallContent"/> instead, which
    /// <c>ShoppingAssistantService</c> reads back into the reply's view model.
    /// </summary>
    private static readonly AIFunctionDeclaration RenderProductCardTool = AIFunctionFactory.CreateDeclaration(
        name: AssistantToolNames.RenderProductCard,
        description: "Shows the shopper a card for one or more products you have just looked up, instead of " +
            "only describing them in prose.",
        jsonSchema: AIJsonUtilities.CreateJsonSchema(type: typeof(RenderProductCardArgs)));

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        string threadId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using AGUIChatClient chatClient = new(
            options: new AGUIChatClientOptions(
                httpClient: httpClient,
                endpoint: ApiRoutes.Agent.ShoppingAssistant));

        // ConversationId is what the AG-UI client carries inward as the thread id. It deliberately
        // keeps sending the full history alongside it, which is why the caller passes both.
        ChatOptions chatOptions = new()
        {
            ConversationId = threadId,
            Tools = [RenderProductCardTool],
        };

        IAsyncEnumerable<ChatResponseUpdate> updates = chatClient.GetStreamingResponseAsync(
            messages: messages,
            options: chatOptions,
            cancellationToken: cancellationToken);

        await foreach (ChatResponseUpdate update in updates.WithCancellation(cancellationToken: cancellationToken))
        {
            yield return update;
        }
    }
}
