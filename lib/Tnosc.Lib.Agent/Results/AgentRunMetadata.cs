// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.Lib.Agent.Results;

/// <summary>
/// What happened during an agent run, aside from the answer itself.
/// </summary>
/// <param name="AgentName">The name of the agent that ran.</param>
/// <param name="RunId">The identifier of this individual run.</param>
/// <param name="ThreadId">The conversation this run continued, if any.</param>
/// <param name="ToolCalls">The tools the agent invoked, in the order it invoked them.</param>
/// <param name="Usage">What the run consumed, as reported by the provider.</param>
/// <remarks>
/// <see cref="ThreadId"/> is a chain-resume identifier and <strong>never an authorization token</strong>.
/// It arrives from the wire, so anything that persists conversations keyed on it must compose the
/// caller's identity into that key — otherwise knowing another caller's thread identifier is enough
/// to resume their conversation.
/// </remarks>
public sealed record AgentRunMetadata(
    string AgentName,
    string RunId,
    string? ThreadId,
    IReadOnlyList<ToolCallRecord> ToolCalls,
    AgentUsage Usage);
