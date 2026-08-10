// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tnosc.Lib.Application.Exceptions;

namespace Tnosc.Lib.Host.Middleware;

/// <summary>
/// Global exception handler that catches any exception escaping the request pipeline and
/// writes it out as an RFC 9457 problem details response via <see cref="IProblemDetailsService"/>.
/// </summary>
/// <remarks>
/// Maps <c>Tnosc.Lib.Application.Exceptions.BaseException</c> subtypes to their corresponding
/// HTTP status code; any other exception falls back to <c>500 Internal Server Error</c>.
/// </remarks>
/// <param name="problemDetailsService">Writes the resulting problem details to the response.</param>
/// <param name="logger">The logger used to record the unhandled exception.</param>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int StatusCode, string Title) problem = MapToProblem(exception: exception);

        logger.LogError(
            exception: exception,
            message: "Unhandled exception processing {Method} {Path} -> {StatusCode}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            problem.StatusCode);

        httpContext.Response.StatusCode = problem.StatusCode;

        return await problemDetailsService.TryWriteAsync(context: new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = problem.StatusCode,
                Title = problem.Title,
                Detail = exception.Message,
                Type = $"https://httpstatuses.io/{problem.StatusCode}",
                Extensions =
                {
                    ["errorCode"] = (exception as BaseException)?.ErrorCode,
                    ["traceId"] = (exception as BaseException)?.CorrelationId ?? httpContext.TraceIdentifier,
                },
            },
        });
    }

    private static (int StatusCode, string Title) MapToProblem(Exception exception) =>
        exception switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, "Resource not found"),
            UnauthorizedException => ((int)HttpStatusCode.Unauthorized, "Unauthorized"),
            InvalidRequestException => ((int)HttpStatusCode.BadRequest, "Invalid request"),
            ConflictException => ((int)HttpStatusCode.Conflict, "Conflict"),
            TransientFailureException => ((int)HttpStatusCode.ServiceUnavailable, "Service temporarily unavailable"),
            BaseException => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred"),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred"),
        };
}
