// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Agent.Results;

/// <summary>
/// What one agent run consumed, as reported by the provider.
/// </summary>
/// <param name="InputTokens">Tokens consumed by the prompt, or <see langword="null"/> if unreported.</param>
/// <param name="OutputTokens">Tokens produced in the answer, or <see langword="null"/> if unreported.</param>
/// <remarks>
/// Both counts are nullable because not every provider reports usage, and a streamed run may not
/// report it until the stream completes. A zero would be a lie in those cases; absence is honest.
/// </remarks>
public sealed record AgentUsage(long? InputTokens, long? OutputTokens)
{
    /// <summary>
    /// Gets a usage record for a run whose provider reported nothing.
    /// </summary>
    public static AgentUsage Unknown { get; } = new(InputTokens: null, OutputTokens: null);
}
