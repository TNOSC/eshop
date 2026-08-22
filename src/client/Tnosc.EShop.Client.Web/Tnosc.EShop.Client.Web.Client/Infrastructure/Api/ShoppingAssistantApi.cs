// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AGUI.Client;
using Microsoft.Extensions.AI;
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
        ChatOptions chatOptions = new() { ConversationId = threadId };

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
