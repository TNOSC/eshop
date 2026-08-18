// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Web.Contracts;

namespace Tnosc.Lib.Web.Results;

/// <summary>
/// The outcome of an API call that returns no value on success — the client-side mirror of the
/// server's <c>Result</c> discipline for 204 endpoints. No client method throws for a non-success
/// status; callers branch on <see cref="IsSuccess"/> instead.
/// </summary>
public sealed class ClientResult
{
    private ClientResult(bool isSuccess, ClientProblem? problem)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    /// <summary>Gets a value indicating whether the call succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the problem details, populated only when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public ClientProblem? Problem { get; }

    /// <summary>Builds a successful result.</summary>
    public static ClientResult Success() => new(isSuccess: true, problem: null);

    /// <summary>Builds a failed result carrying the server's problem details.</summary>
    /// <param name="problem">The problem details describing why the call failed.</param>
    public static ClientResult Failure(ClientProblem problem) => new(isSuccess: false, problem: problem);
}
