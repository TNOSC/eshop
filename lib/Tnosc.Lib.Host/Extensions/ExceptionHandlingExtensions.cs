// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Tnosc.Lib.Host.Middleware;

namespace Tnosc.Lib.Host.Extensions;

/// <summary>
/// Provides extension methods to register the global exception handling pipeline.
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Registers <see cref="GlobalExceptionHandler"/> and the ASP.NET Core problem details
    /// service used to translate unhandled exceptions into RFC 9457 problem details responses.
    /// </summary>
    /// <param name="services">The service collection to which the exception handling services will be added.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// Callers must also invoke <c>app.UseExceptionHandler()</c> in the request pipeline for
    /// the registered handler to take effect.
    /// </remarks>
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
