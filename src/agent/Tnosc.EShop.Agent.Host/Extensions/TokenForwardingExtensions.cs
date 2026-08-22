// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.EShop.Agent.Host.Authentication;

namespace Tnosc.EShop.Agent.Host.Extensions;

/// <summary>
/// Wires up <see cref="TokenForwarder"/>, which reattaches the caller's bearer token to every
/// outbound call the eShop API typed client makes.
/// </summary>
internal static class TokenForwardingExtensions
{
    /// <summary>
    /// Registers <see cref="TokenForwarder"/> and the <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> it depends on.
    /// </summary>
    /// <param name="builder">The host application builder to register services on.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    internal static IHostApplicationBuilder AddTokenForwarding(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<TokenForwarder>();

        return builder;
    }
}
