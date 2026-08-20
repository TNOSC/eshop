// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Mcp.Application.Products.Ports;
using Tnosc.EShop.Mcp.Infrastructure.External.Products;

namespace Tnosc.EShop.Mcp.Infrastructure.External.Extensions;

/// <summary>
/// Composition root for <c>Mcp.Infrastructure.External</c> — everything that talks to a system
/// outside this process over a client. The eShop API is the first tenant.
/// </summary>
public static class McpInfrastructureExtensions
{
    /// <summary>
    /// Registers the External layer's services: <see cref="IProductsClient"/>, backed by a typed
    /// <see cref="System.Net.Http.HttpClient"/> pointed at the eShop API host.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddMcpInfrastructureExternal(this IServiceCollection services)
    {
#pragma warning disable S1075 // Not a hardcoded endpoint — "eshop-host" is a service-discovery name resolved by AddServiceDefaults/AddServiceDiscovery, not a literal address.
        services.AddHttpClient<IProductsClient, ProductsClient>(
                configureClient: static client => client.BaseAddress = new Uri(uriString: "https+http://eshop-host/"))
            .AddStandardResilienceHandler();
#pragma warning restore S1075

        return services;
    }
}
