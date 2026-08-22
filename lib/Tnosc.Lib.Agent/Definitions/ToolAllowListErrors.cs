// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Every way a candidate <see cref="ToolAllowList"/> can break its invariants.
/// </summary>
public static class ToolAllowListErrors
{
    /// <summary>
    /// Gets the error returned when a tool name is blank.
    /// </summary>
    public static Error NameEmpty => Error.Validation(
        code: "ToolAllowList.NameEmpty",
        description: "A tool name in an allow-list cannot be blank.");

    /// <summary>
    /// Gets the error returned when the same tool name appears more than once.
    /// </summary>
    /// <remarks>
    /// A duplicate is rejected rather than collapsed because it usually means two people edited the
    /// list without seeing each other's entry, and silently de-duplicating hides that.
    /// </remarks>
    public static Error Duplicate => Error.Validation(
        code: "ToolAllowList.Duplicate",
        description: "A tool name cannot appear more than once in an allow-list.");
}
