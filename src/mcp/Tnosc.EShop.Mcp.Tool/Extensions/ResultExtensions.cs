// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Mcp.Tool.Extensions;

/// <summary>
/// Maps a <see cref="Result{TValue}"/> to a <see cref="ToolResult{TValue}"/> — the one place a
/// <see cref="Result{TValue}"/> becomes the structured success/failure envelope a tool method
/// returns, so a tool never has to decide how a failure is reported to the client itself.
/// </summary>
internal static class ResultExtensions
{
    /// <summary>
    /// Converts <paramref name="result"/> into its structured <see cref="ToolResult{TValue}"/>
    /// equivalent, carrying the first error's code and description on failure.
    /// </summary>
    /// <typeparam name="TValue">The type of the value a successful result carries.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>A successful or failed <see cref="ToolResult{TValue}"/> mirroring <paramref name="result"/>.</returns>
    public static ToolResult<TValue> ToToolResult<TValue>(this Result<TValue> result) =>
        result.IsError
            ? ToolResult<TValue>.Fail(errorCode: result.FirstError.Code, errorMessage: result.FirstError.Description)
            : ToolResult<TValue>.Ok(data: result.Value);
}
