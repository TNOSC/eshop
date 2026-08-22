// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Tnosc.Lib.Agent.Results;

namespace Tnosc.EShop.Agent.Application.Running;

/// <summary>
/// Projects the agent framework's own response shape onto this solution's <see cref="AgentResult"/>.
/// </summary>
internal static class AgentResponseMapper
{
    /// <summary>
    /// Builds the run metadata from a completed response.
    /// </summary>
    /// <param name="agentName">The agent that ran.</param>
    /// <param name="responseId">The provider's identifier for this run.</param>
    /// <param name="threadId">The conversation this run continued, if any.</param>
    /// <param name="messages">The messages the run produced.</param>
    /// <param name="usage">What the provider reported the run consumed.</param>
    /// <returns>The mapped metadata.</returns>
    public static AgentRunMetadata ToMetadata(
        string agentName,
        string? responseId,
        string? threadId,
        IEnumerable<ChatMessage> messages,
        UsageDetails? usage) =>
        new(AgentName: agentName,
            RunId: responseId ?? string.Empty,
            ThreadId: threadId,
            ToolCalls: ToToolCalls(messages: messages),
            Usage: ToUsage(usage: usage));

    private static List<ToolCallRecord> ToToolCalls(IEnumerable<ChatMessage> messages)
    {
        // A call and its result arrive as separate contents, correlated by CallId, and the result may
        // land in a later message than the call. Collect the names first, then mark the ones whose
        // result carried an exception — that ordering is what makes a failed call still show up.
        Dictionary<string, ToolCallRecord> byCallId = new(comparer: System.StringComparer.Ordinal);
        List<string> order = [];

        foreach (ChatMessage message in messages)
        {
            foreach (AIContent content in message.Contents)
            {
                if (content is FunctionCallContent call)
                {
                    if (byCallId.TryAdd(key: call.CallId, value: new ToolCallRecord(Name: call.Name, Succeeded: true)))
                    {
                        order.Add(item: call.CallId);
                    }
                }
                else if (content is FunctionResultContent result &&
                         byCallId.TryGetValue(key: result.CallId, value: out ToolCallRecord? record))
                {
                    byCallId[result.CallId] = record with { Succeeded = result.Exception is null };
                }
            }
        }

        List<ToolCallRecord> calls = new(capacity: order.Count);

        foreach (string callId in order)
        {
            calls.Add(item: byCallId[callId]);
        }

        return calls;
    }

    private static AgentUsage ToUsage(UsageDetails? usage) =>
        usage is null
            ? AgentUsage.Unknown
            : new AgentUsage(InputTokens: usage.InputTokenCount, OutputTokens: usage.OutputTokenCount);
}
