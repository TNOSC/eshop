// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.Lib.Api;

/// <summary>
/// Provides custom HTTP results for mapping domain <see cref="Result"/> objects to ASP.NET Core <see cref="IResult"/> instances.
/// </summary>
public static class CustomResults
{
    /// <summary>
    /// Converts a domain <see cref="Result"/> that represents an error into an <see cref="IResult"/>
    /// that will produce an RFC 7807 Problem Details response.
    /// </summary>
    /// <param name="result">The domain result containing one or more errors. Must represent an error (IsSuccess == false).</param>
    /// <returns>An <see cref="IResult"/> which, when executed, writes a ProblemDetails HTTP response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="result"/> indicates success.</exception>
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException();
        }

        return Results.Problem(
            title: GetTitle(result.FirstError),
            detail: GetDetail(result.FirstError),
            type: GetType(result.FirstError.Type),
            statusCode: GetStatusCode(result.FirstError),
            extensions: GetErrors(result));
    }
    private static string GetTitle(Error error) =>
        error.Type switch
        {
            ErrorType.Conflict => error.Code,
            ErrorType.Validation => error.Code,
            ErrorType.NotFound => error.Code,
            ErrorType.Unauthorized => error.Code,
            ErrorType.Forbidden => error.Code,
            ErrorType.Failure => error.Code,
            ErrorType.Unexpected => error.Code,
            ErrorType.Custom => error.Code,
            _ => "Server failure"
        };

    private static string GetDetail(Error error) =>
        error.Type switch
        {
            ErrorType.Conflict => error.Description,
            ErrorType.Validation => error.Description,
            ErrorType.NotFound => error.Description,
            ErrorType.Unauthorized => error.Description,
            ErrorType.Forbidden => error.Description,
            ErrorType.Failure => error.Description,
            ErrorType.Unexpected => error.Description,
            ErrorType.Custom => error.Description,
            _ => "An unexpected error occurred"
        };

    private static string GetType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Unauthorized => "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
            ErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            ErrorType.Failure => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            ErrorType.Unexpected => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            ErrorType.Custom => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

    private static int GetStatusCode(Error error) =>
        error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorType.Custom => IsValidHttpStatusCode(error.NumericType)
                ? error.NumericType
                : StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

    private static bool IsValidHttpStatusCode(int statusCode) =>
        statusCode is >= 100 and <= 599;

    private static Dictionary<string, object?>? GetErrors(Result result)
    {
        if (result.FirstError.Type is not ErrorType.Validation)
        {
            return null;
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { "errors", result.Errors.ToDictionary(
                    k => k.Code,
                    v => new[] { v.Description },
                    StringComparer.Ordinal)
                },
            };
    }
}
