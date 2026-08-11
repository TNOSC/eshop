// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Api.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="Result"/> and <see cref="Result{TIn}"/> instances.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes <paramref name="onSuccess"/> when <paramref name="result"/> represents success,
    /// otherwise executes <paramref name="onFailure"/> and returns its value.
    /// </summary>
    /// <typeparam name="TOut">The return type of the match functions.</typeparam>
    /// <param name="result">The result to match on.</param>
    /// <param name="onSuccess">Function to execute when <paramref name="result"/> is successful.</param>
    /// <param name="onFailure">Function to execute when <paramref name="result"/> is a failure. Receives the original result.</param>
    /// <returns>The value returned by either <paramref name="onSuccess"/> or <paramref name="onFailure"/>.</returns>
    public static TOut Match<TOut>(
        this Result result,
        Func<TOut> onSuccess,
        Func<Result, TOut> onFailure) =>
            result.IsSuccess ? onSuccess() : onFailure(result);

    /// <summary>
    /// Executes <paramref name="onSuccess"/> when <paramref name="result"/> represents success,
    /// otherwise executes <paramref name="onFailure"/> and returns its value.
    /// </summary>
    /// <typeparam name="TIn">Type of the value contained in the successful result.</typeparam>
    /// <typeparam name="TOut">The return type of the match functions.</typeparam>
    /// <param name="result">The result to match on.</param>
    /// <param name="onSuccess">Function to execute when <paramref name="result"/> is successful. Receives the contained value.</param>
    /// <param name="onFailure">Function to execute when <paramref name="result"/> is a failure. Receives the original result.</param>
    /// <returns>The value returned by either <paramref name="onSuccess"/> or <paramref name="onFailure"/>.</returns>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Result<TIn>, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result);
}
