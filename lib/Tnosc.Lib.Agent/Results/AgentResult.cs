// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Agent.Results;

/// <summary>
/// The outcome of an agent run that completed: what the agent said, and what it did to say it.
/// </summary>
/// <param name="Text">The agent's answer in prose.</param>
/// <param name="Metadata">What happened during the run, aside from the answer.</param>
/// <remarks>
/// <para>
/// This is a <em>success value</em>, carried inside the same result envelope every other operation
/// in the codebase uses — it is not a parallel result type. Keeping one error vocabulary means the
/// existing mappings from a result to an HTTP response or a tool response keep working unchanged.
/// </para>
/// <para>
/// The line between the two is worth stating plainly. An enclosing failure means the run could not
/// happen or could not finish: no such agent, the model was unreachable, the call timed out, the
/// answer would not bind to the declared shape. Reaching an <see cref="AgentResult"/> at all means
/// the agent ran and answered — so a tool refusing the caller, or the agent saying it cannot help,
/// is <strong>content</strong>, not an error. Treating a refused tool as a failure would turn a
/// correctly enforced permission into a 500.
/// </para>
/// </remarks>
public record AgentResult(string Text, AgentRunMetadata Metadata);

/// <summary>
/// The outcome of an agent run that was asked for a structured answer.
/// </summary>
/// <typeparam name="TOutput">The shape the agent's answer was bound to.</typeparam>
/// <param name="Text">The agent's answer in prose.</param>
/// <param name="Output">
/// The bound structured answer, or <see langword="null"/> when the agent's definition declared no
/// output contract.
/// </param>
/// <param name="Metadata">What happened during the run, aside from the answer.</param>
/// <remarks>
/// <see cref="Output"/> is nullable because structured output is opt-in: prose is the common case,
/// and requiring every agent to declare a shape would buy fragility for the ones that never needed
/// one. A run whose declared shape failed to bind does not arrive here at all — that is an enclosing
/// failure, because silently handing back a null the caller asked to be populated is worse than
/// saying so.
/// </remarks>
public sealed record AgentResult<TOutput>(
    string Text,
    TOutput? Output,
    AgentRunMetadata Metadata) : AgentResult(Text: Text, Metadata: Metadata);
