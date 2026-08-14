// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Contracts.Common;

/// <summary>
/// The ProblemDetails shape returned by the server, covering both the <c>Result</c>-error variant
/// (<c>Title</c> is the error code, <c>Detail</c> the description, and for
/// <c>ErrorType.Validation</c> an <see cref="Errors"/> dictionary keyed by error code) and the
/// unhandled-exception variant (<see cref="ErrorCode"/> and <see cref="TraceId"/> extensions).
/// </summary>
public sealed record ApiProblem(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    string? Instance,
    IReadOnlyDictionary<string, string[]>? Errors,
    string? ErrorCode,
    string? TraceId)
{
    /// <summary>
    /// Builds a fallback <see cref="ApiProblem"/> for a non-success response whose body could not be
    /// parsed as ProblemDetails at all.
    /// </summary>
    /// <param name="status">The HTTP status code of the response.</param>
    public static ApiProblem FromStatus(int status) =>
        new(
            Type: null,
            Title: null,
            Status: status,
            Detail: null,
            Instance: null,
            Errors: null,
            ErrorCode: null,
            TraceId: null);
}
