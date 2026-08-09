// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Observabilities;

namespace Tnosc.Lib.Host.Middleware;

/// <summary>
/// Middleware that adds a CorrelationId value to the logging scope for each request.
/// </summary>
/// <param name="next">The request delegate to invoke the next middleware in the pipeline.</param>
/// <param name="logger">The logger used to create the scoped logging context.</param>
public class RequestContextMiddleware(
    RequestDelegate next,
    ILogger<RequestContextMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "Correlation-Id";

    /// <summary>
    /// Processes the HTTP request within a logging scope that includes a correlation id,
    /// and makes that correlation id the default for any exception thrown while handling the request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="userContext">Provides information about the current caller.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        string correlationId = GetCorrelationId(context);

        var scopeState = new Dictionary<string, object>(System.StringComparer.Ordinal)
        {
            ["CorrelationId"] = correlationId,
        };

        if (userContext.UserId is not null)
        {
            scopeState["UserId"] = userContext.UserId;
        }

        CorrelationIdContext.Current = correlationId;
        try
        {
            using (logger.BeginScope(scopeState))
            {
                await next.Invoke(context);
            }
        }
        finally
        {
            CorrelationIdContext.Current = null;
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            CorrelationIdHeaderName,
            out StringValues correlationId);

        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }
}
