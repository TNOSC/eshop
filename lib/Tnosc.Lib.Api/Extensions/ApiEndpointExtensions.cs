// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tnosc.Lib.Api.Abstractions;

namespace Tnosc.Lib.Api.Extensions;

/// <summary>
/// Provides extension methods to register and map API endpoints.
/// </summary>
public static class ApiEndpointExtensions
{
    /// <summary>
    /// Registers all non-abstract, non-interface types that implement <see cref="IApiEndpoint"/>
    /// found in the specified <paramref name="assembly"/> as transient <see cref="IApiEndpoint"/> services.
    /// </summary>
    /// <param name="services">The service collection to which endpoint services will be added.</param>
    /// <param name="assembly">The assembly to scan for <see cref="IApiEndpoint"/> implementations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddApiEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] serviceDescriptors = [.. assembly
            .DefinedTypes
            .Where(predicate: type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(targetType: typeof(IApiEndpoint)))
            .Select(selector: type => ServiceDescriptor.Transient(service: typeof(IApiEndpoint), implementationType: type))];

        services.TryAddEnumerable(descriptors: serviceDescriptors);

        return services;
    }

    /// <summary>
    /// Resolves registered <see cref="IApiEndpoint"/> implementations from dependency injection
    /// and calls <see cref="IApiEndpoint.MapEndpoint(WebApplication)"/> on each to register their routes.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> used to register endpoints.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance for chaining.</returns>
    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        IEnumerable<IApiEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IApiEndpoint>>();

        foreach (IApiEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(app: app);
        }

        return app;
    }

    /// <summary>
    /// Adds an authorization requirement for the specified permission to the route handler builder.
    /// </summary>
    /// <param name="app">The route handler builder to configure.</param>
    /// <param name="permission">The permission name required to access the endpoint.</param>
    /// <returns>The same <see cref="RouteHandlerBuilder"/> instance to allow chaining.</returns>
    public static RouteHandlerBuilder HasPermission(this RouteHandlerBuilder app, string permission)
    {
        return app.RequireAuthorization(policyNames: permission);
    }
}
