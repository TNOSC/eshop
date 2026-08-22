// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Every way a candidate <see cref="Instructions"/> can break its invariant.
/// </summary>
public static class InstructionsErrors
{
    /// <summary>
    /// Gets the error returned when no instruction text was supplied.
    /// </summary>
    public static Error Empty => Error.Validation(
        code: "Instructions.Empty",
        description: "An agent requires instructions.");

    /// <summary>
    /// Gets the error returned when instruction text exceeds <see cref="Instructions.MaxLength"/>.
    /// </summary>
    public static Error TooLong => Error.Validation(
        code: "Instructions.TooLong",
        description: $"Instructions must be at most {Instructions.MaxLength} characters long.");
}
