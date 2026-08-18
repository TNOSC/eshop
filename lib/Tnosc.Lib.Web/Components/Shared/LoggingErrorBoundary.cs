// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Tnosc.Lib.Web.Components.Shared;

/// <summary>
/// An <see cref="ErrorBoundary"/> that logs the caught exception and notifies
/// <see cref="StatefulBoundary"/> so it can surface <see cref="ComponentState.Error"/> to its parent.
/// </summary>
public sealed class LoggingErrorBoundary(ILogger<LoggingErrorBoundary> logger) : ErrorBoundary
{
    /// <summary>Gets or sets the callback invoked when the boundary catches an exception.</summary>
    public Action<Exception>? OnError { get; set; }

    /// <inheritdoc />
    protected override Task OnErrorAsync(Exception exception)
    {
        logger.LogError(
            exception: exception,
            message: "StatefulBoundary caught an unhandled exception while rendering.");
        OnError?.Invoke(exception);
        return base.OnErrorAsync(exception);
    }
}
