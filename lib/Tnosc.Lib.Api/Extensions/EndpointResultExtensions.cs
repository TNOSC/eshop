// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Http;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.Lib.Api.Extensions;

/// <summary>
/// Provides extension methods that map domain <see cref="Result"/> and <see cref="Result{TValue}"/>
/// instances directly to ASP.NET Core HTTP results, keeping endpoint handlers free of repeated
/// <see cref="ResultExtensions.Match{TIn, TOut}(Result{TIn}, Func{TIn, TOut}, Func{Result{TIn}, TOut})"/>
/// / <see cref="CustomResults.Problem(Result)"/> ceremony.
/// </summary>
/// <remarks>
/// Throughout this type, the unqualified name <c>IResult</c> refers to
/// <see cref="Microsoft.AspNetCore.Http.IResult"/> via an explicit <c>using</c> alias, to disambiguate
/// it from the domain type <see cref="Tnosc.Lib.Domain.Results.IResult"/> implemented by <see cref="Result"/>.
/// </remarks>
public static class EndpointResultExtensions
{
    /// <summary>
    /// Maps a domain <see cref="Result"/> to an HTTP result: success produces <paramref name="successStatusCode"/>
    /// with no body, failure produces an RFC 7807 ProblemDetails response.
    /// </summary>
    /// <param name="result">The domain result to map.</param>
    /// <param name="successStatusCode">The HTTP status code returned when <paramref name="result"/> is successful. Defaults to 204 No Content.</param>
    /// <returns>An <see cref="IResult"/> representing <paramref name="result"/>.</returns>
    public static IResult ToHttp(this Result result, int successStatusCode = StatusCodes.Status204NoContent) =>
        result.Match(
            onSuccess: () => Results.StatusCode(statusCode: successStatusCode),
            onFailure: CustomResults.Problem);

    /// <summary>
    /// Maps a domain <see cref="Result{TValue}"/> to an HTTP result: success delegates to
    /// <paramref name="onSuccess"/> with the contained value, failure produces an RFC 7807 ProblemDetails response.
    /// </summary>
    /// <typeparam name="TValue">The type of the value contained in a successful result.</typeparam>
    /// <param name="result">The domain result to map.</param>
    /// <param name="onSuccess">Builds the HTTP result for the successful value.</param>
    /// <returns>An <see cref="IResult"/> representing <paramref name="result"/>.</returns>
    public static IResult ToHttp<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.Match(
            onSuccess: onSuccess,
            onFailure: CustomResults.Problem);

    /// <summary>
    /// Maps a domain <see cref="Result{TValue}"/> to a 201 Created HTTP result with a <c>Location</c> header,
    /// or to an RFC 7807 ProblemDetails response on failure.
    /// </summary>
    /// <typeparam name="TValue">The type of the value contained in a successful result.</typeparam>
    /// <param name="result">The domain result to map.</param>
    /// <param name="location">Builds the <c>Location</c> header value from the successful value.</param>
    /// <returns>An <see cref="IResult"/> representing <paramref name="result"/>.</returns>
    public static IResult ToCreated<TValue>(this Result<TValue> result, Func<TValue, string> location) =>
        result.Match(
            onSuccess: value => Results.Created(uri: location(value), value: value),
            onFailure: CustomResults.Problem);
}
