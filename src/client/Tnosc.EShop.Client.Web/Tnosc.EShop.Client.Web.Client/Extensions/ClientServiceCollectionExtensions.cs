// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

namespace Tnosc.EShop.Client.Web.Client.Extensions;

/// <summary>
/// Registers the typed API clients shared by both hosts — the WASM <c>.Client</c> project talking
/// straight to its own origin's BFF, and the server host talking to <c>eshop-host</c> over service
/// discovery. The difference between the two is entirely the <paramref name="baseAddress"/> and the
/// optional <paramref name="configure"/> callback; nothing else about a typed client changes.
/// </summary>
public static class ClientServiceCollectionExtensions
{
    /// <summary>Adds every typed eShop API client to the container.</summary>
    /// <param name="services">The service collection to add the clients to.</param>
    /// <param name="baseAddress">
    /// The base address every typed client is configured with. Must end in a trailing slash — without
    /// one, <see cref="Uri"/> replaces the last path segment of a relative request URI instead of
    /// appending to it.
    /// </param>
    /// <param name="configure">
    /// An optional callback applied to every typed client's <see cref="IHttpClientBuilder"/>. Used by
    /// the server host to attach <c>ServerAccessTokenHandler</c> to just these clients, rather than to
    /// every <see cref="System.Net.Http.HttpClient"/> in the host via
    /// <c>ConfigureHttpClientDefaults</c>.
    /// </param>
    public static IServiceCollection AddEShopApiClients(
        this IServiceCollection services,
        Uri baseAddress,
        Action<IHttpClientBuilder>? configure = null)
    {
        IHttpClientBuilder catalog = services.AddHttpClient<ICatalogApi, CatalogApi>(
            name: ApiClientNames.Catalog,
            configureClient: client => client.BaseAddress = baseAddress);
        configure?.Invoke(obj: catalog);

        return services;
    }
}
