// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Host.Contexts;

namespace Tnosc.Lib.Host.Extensions;

/// <summary>
/// Provides extension methods to register the HTTP-backed <see cref="IUserContext"/>.
/// </summary>
public static class UserContextExtensions
{
    /// <summary>
    /// Registers <see cref="IUserContext"/> and its supporting <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
    /// </summary>
    /// <param name="services">The service collection to which the user context will be added.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<IUserContext, HttpUserContext>();

        return services;
    }
}
