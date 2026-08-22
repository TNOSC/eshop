// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.AI;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// The shopping assistant's conversation client. Unlike the other typed clients this one streams, so
/// it yields updates instead of returning a <c>ClientResult</c> — the owning feature service is what
/// turns the stream into a result and a view model.
/// </summary>
public interface IShoppingAssistantApi
{
    /// <summary>Sends a conversation turn and streams the assistant's reply back as it arrives.</summary>
    /// <param name="messages">
    /// The full conversation so far, oldest first. AG-UI re-sends the whole history on every turn, so
    /// this is not just the newest message.
    /// </param>
    /// <param name="threadId">
    /// The conversation's continuation identifier. It resumes a conversation and is not a credential —
    /// the agent host isolates conversations by the caller's own identity as well.
    /// </param>
    /// <param name="cancellationToken">The token observed while the stream is open.</param>
    /// <returns>The reply's updates, in order.</returns>
    IAsyncEnumerable<ChatResponseUpdate> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        string threadId,
        CancellationToken cancellationToken);
}
