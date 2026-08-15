// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Client.Web.Contracts.Common;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// The outcome of an API call that returns no value on success — the client-side mirror of the
/// server's <c>Result</c> discipline for 204 endpoints. No client method throws for a non-success
/// status; callers branch on <see cref="IsSuccess"/> instead.
/// </summary>
public sealed class ApiResult
{
    private ApiResult(bool isSuccess, ApiProblem? problem)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    /// <summary>Gets a value indicating whether the call succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the problem details, populated only when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public ApiProblem? Problem { get; }

    /// <summary>Builds a successful result.</summary>
    public static ApiResult Success() => new(isSuccess: true, problem: null);

    /// <summary>Builds a failed result carrying the server's problem details.</summary>
    /// <param name="problem">The problem details describing why the call failed.</param>
    public static ApiResult Failure(ApiProblem problem) => new(isSuccess: false, problem: problem);
}
