// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Every way a candidate <see cref="AgentName"/> can break the format invariant.
/// </summary>
public static class AgentNameErrors
{
    /// <summary>
    /// Gets the error returned when no agent name was supplied.
    /// </summary>
    public static Error Empty => Error.Validation(
        code: "AgentName.Empty",
        description: "An agent name is required.");

    /// <summary>
    /// Gets the error returned when an agent name exceeds <see cref="AgentName.MaxLength"/>.
    /// </summary>
    public static Error TooLong => Error.Validation(
        code: "AgentName.TooLong",
        description: $"An agent name must be at most {AgentName.MaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when an agent name is not lowercase kebab-case.
    /// </summary>
    public static Error InvalidFormat => Error.Validation(
        code: "AgentName.InvalidFormat",
        description: "An agent name may contain only lowercase letters, digits and inner dashes.");
}
