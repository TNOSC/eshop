// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Web.Contracts;

namespace Tnosc.Lib.Web.Results;

/// <summary>
/// The outcome of an API call that returns a <typeparamref name="TValue"/> on success — the
/// client-side mirror of the server's <c>Result{TValue}</c> discipline. No client method throws for
/// a non-success status; callers branch on <see cref="IsSuccess"/> instead.
/// </summary>
/// <typeparam name="TValue">The type of the value returned on success.</typeparam>
public sealed class ClientResult<TValue>
{
    private ClientResult(bool isSuccess, TValue value, ClientProblem? problem)
    {
        IsSuccess = isSuccess;
        Value = value;
        Problem = problem;
    }

    /// <summary>Gets a value indicating whether the call succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the value. Only meaningful when <see cref="IsSuccess"/> is <see langword="true"/>.</summary>
    public TValue Value { get; }

    /// <summary>Gets the problem details, populated only when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public ClientProblem? Problem { get; }

    /// <summary>Builds a successful result.</summary>
    /// <param name="value">The value returned by the call.</param>
    public static ClientResult<TValue> Success(TValue value) => new(isSuccess: true, value: value, problem: null);

    /// <summary>Builds a failed result carrying the server's problem details.</summary>
    /// <param name="problem">The problem details describing why the call failed.</param>
    public static ClientResult<TValue> Failure(ClientProblem problem) => new(isSuccess: false, value: default!, problem: problem);
}
