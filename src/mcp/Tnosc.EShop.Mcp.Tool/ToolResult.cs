// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Mcp.Tool;

/// <summary>
/// The structured envelope every tool in this project returns. A caller inspects
/// <see cref="Success"/> rather than relying on the MCP protocol's error signal — an expected
/// failure (invalid input, a rejected command, an unreachable catalog) is still a normal,
/// structured response, not a thrown exception.
/// </summary>
/// <typeparam name="TValue">The type of the value carried by a successful result.</typeparam>
/// <param name="Success">Whether the call succeeded.</param>
/// <param name="Data">The value produced by a successful call; <see langword="null"/> on failure.</param>
/// <param name="ErrorCode">A stable machine-readable failure reason; <see langword="null"/> on success.</param>
/// <param name="ErrorMessage">A human-readable description of the failure; <see langword="null"/> on success.</param>
public sealed record ToolResult<TValue>(
    bool Success,
    TValue? Data,
    string? ErrorCode,
    string? ErrorMessage)
{
    /// <summary>Builds a successful result carrying <paramref name="data"/>.</summary>
    /// <param name="data">The value the call produced.</param>
    /// <returns>A successful <see cref="ToolResult{TValue}"/>.</returns>
    public static ToolResult<TValue> Ok(TValue data) =>
        new(Success: true, Data: data, ErrorCode: null, ErrorMessage: null);

    /// <summary>Builds a failed result describing why the call did not succeed.</summary>
    /// <param name="errorCode">A stable machine-readable failure reason.</param>
    /// <param name="errorMessage">A human-readable description of the failure.</param>
    /// <returns>A failed <see cref="ToolResult{TValue}"/>.</returns>
    public static ToolResult<TValue> Fail(string errorCode, string errorMessage) =>
        new(Success: false, Data: default, ErrorCode: errorCode, ErrorMessage: errorMessage);
}
