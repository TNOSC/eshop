// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using ModelContextProtocol;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Mcp.Tool.Extensions;

/// <summary>
/// Maps a <see cref="Result{TValue}"/> to a tool's return value or an <see cref="McpException"/> —
/// the one place a <see cref="Result{TValue}"/> becomes the MCP SDK's structured-failure signal,
/// so a tool method itself never has to decide how a failure is reported to the client.
/// </summary>
internal static class ResultExtensions
{
    /// <summary>
    /// Returns <paramref name="result"/>'s value, or throws an <see cref="McpException"/> carrying
    /// the first error's description so the calling agent sees the precise reason for the failure.
    /// </summary>
    /// <typeparam name="TValue">The type of the value a successful result carries.</typeparam>
    /// <param name="result">The result to unwrap.</param>
    /// <returns>The result's value, when successful.</returns>
    /// <exception cref="McpException">Thrown when <paramref name="result"/> is a failure.</exception>
    public static TValue GetValueOrThrowMcpException<TValue>(this Result<TValue> result) =>
        result.IsError
            ? throw new McpException(message: result.FirstError.Description)
            : result.Value;
}
