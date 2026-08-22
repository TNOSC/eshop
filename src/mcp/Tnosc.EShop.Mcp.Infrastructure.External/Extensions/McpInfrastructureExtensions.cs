// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Mcp.Application.Ports;

namespace Tnosc.EShop.Mcp.Infrastructure.External.Extensions;

/// <summary>
/// Composition root for <c>Mcp.Infrastructure.External</c> — everything that talks to a system
/// outside this process over a client. The eShop API is the first tenant.
/// </summary>
public static class McpInfrastructureExtensions
{
    /// <summary>
    /// Registers the External layer's services: <see cref="IEShopClient"/>, backed by a typed
    /// <see cref="System.Net.Http.HttpClient"/> pointed at the eShop API host.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>
    /// The <see cref="IHttpClientBuilder"/> for the registered <see cref="IEShopClient"/> client, so
    /// the composition root can chain in a message handler — such as the Host's own bearer-token
    /// forwarder — without this layer knowing anything about it.
    /// </returns>
    public static IHttpClientBuilder AddMcpInfrastructureExternal(this IServiceCollection services)
    {
#pragma warning disable S1075 // Not a hardcoded endpoint — "eshop-host" is a service-discovery name resolved by AddServiceDefaults/AddServiceDiscovery, not a literal address.
        IHttpClientBuilder builder = services.AddHttpClient<IEShopClient, EShopClient>(
            configureClient: static client => client.BaseAddress = new Uri(uriString: "https+http://eshop-host/"));
#pragma warning restore S1075

        builder.AddStandardResilienceHandler();

        return builder;
    }
}
