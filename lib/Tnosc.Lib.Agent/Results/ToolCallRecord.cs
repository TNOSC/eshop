// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Agent.Results;

/// <summary>
/// One tool invocation made during an agent run.
/// </summary>
/// <param name="Name">The name of the tool the agent called.</param>
/// <param name="Succeeded">Whether the tool returned a result rather than a failure.</param>
/// <remarks>
/// <para>
/// This record exists so that what an agent <em>did</em> can be asserted on directly. Without it,
/// the only evidence a tool ran is the model's prose, and a test that pattern-matches wording breaks
/// the first time the model phrases an answer differently — which is a flaky test dressed up as a
/// behavioural one.
/// </para>
/// <para>
/// <paramref name="Succeeded"/> being <see langword="false"/> is a normal outcome, not an error: a
/// tool refusing a caller who lacks permission is exactly what a correctly configured system does,
/// and the agent is expected to tell the user so.
/// </para>
/// </remarks>
public sealed record ToolCallRecord(string Name, bool Succeeded);
