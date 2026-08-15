// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
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
using Tnosc.Lib.Application.Contexts;

namespace Tnosc.Lib.Host.Middleware;

/// <summary>
/// Middleware that lifts the per-request header context — correlation id and idempotency key —
/// out of HTTP and into the ambient contexts the inner layers read.
/// </summary>
/// <remarks>
/// This is the only place either header is read. Both are exposed as async-flowing ambient state
/// rather than threaded through command and handler signatures, and both are cleared in a
/// <c>finally</c> so a pooled request thread never carries one request's values into the next.
/// </remarks>
/// <param name="next">The request delegate to invoke the next middleware in the pipeline.</param>
/// <param name="logger">The logger used to create the scoped logging context.</param>
public class RequestContextMiddleware(
    RequestDelegate next,
    ILogger<RequestContextMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "Correlation-Id";
    private const string IdempotencyKeyHeaderName = "Idempotency-Key";

    /// <summary>
    /// Processes the HTTP request within a logging scope that includes a correlation id, makes that
    /// correlation id the default for any exception thrown while handling the request, and publishes
    /// the caller's idempotency key for handlers marked <c>[Idempotent]</c>.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="userContext">Provides information about the current caller.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        string correlationId = GetCorrelationId(context: context);

        var scopeState = new Dictionary<string, object>(comparer: System.StringComparer.Ordinal)
        {
            ["CorrelationId"] = correlationId,
        };

        if (userContext.UserId is not null)
        {
            scopeState["UserId"] = userContext.UserId;
        }

        CorrelationIdContext.Current = correlationId;

        // Left null when the header is absent: an [Idempotent] handler must be able to tell "no key
        // supplied" apart from any key value, because it rejects the request rather than degrading.
        IdempotencyKeyContext.Current = GetIdempotencyKey(context: context);
        try
        {
            using (logger.BeginScope(state: scopeState))
            {
                await next.Invoke(context: context);
            }
        }
        finally
        {
            CorrelationIdContext.Current = null;
            IdempotencyKeyContext.Current = null;
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            key: CorrelationIdHeaderName,
            value: out StringValues correlationId);

        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }

    private static string? GetIdempotencyKey(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            key: IdempotencyKeyHeaderName,
            value: out StringValues idempotencyKey);

        return idempotencyKey.FirstOrDefault();
    }
}
