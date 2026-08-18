// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Basket.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;
using Tnosc.Lib.Web.Api;

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
    /// <c>ConfigureHttpClientDefaults</c>. When omitted — the WASM host's own registration — every
    /// typed client instead gets <see cref="RequestedWithHandler"/> attached, since a WASM caller has
    /// no server-side <c>HttpContext</c> to read a token from and needs the same-origin proof the BFF
    /// proxy checks in its place.
    /// </param>
    public static IServiceCollection AddEShopApiClients(
        this IServiceCollection services,
        Uri baseAddress,
        Action<IHttpClientBuilder>? configure = null)
    {
        services.AddTransient<RequestedWithHandler>();
        services.AddScoped<BasketState>();
        services.AddScoped<IProductsService, ProductsService>();
        services.AddScoped<IProductDetailService, ProductDetailService>();
        services.AddScoped<ICreateProductService, CreateProductService>();
        services.AddScoped<IAdminProductsService, AdminProductsService>();
        services.AddScoped<IUpdateProductPriceService, UpdateProductPriceService>();
        services.AddScoped<IAdjustStockService, AdjustStockService>();
        services.AddScoped<IBasketPageService, BasketPageService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IMyOrdersService, MyOrdersService>();
        services.AddScoped<IOrderDetailService, OrderDetailService>();
        services.AddScoped<IAdminCustomersService, AdminCustomersService>();
        services.AddScoped<IAdminCustomerDetailService, AdminCustomerDetailService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        IHttpClientBuilder catalog = services.AddHttpClient<ICatalogApi, CatalogApi>(
            name: ApiClientNames.Catalog,
            configureClient: client => client.BaseAddress = baseAddress);

        IHttpClientBuilder basket = services.AddHttpClient<IBasketApi, BasketApi>(
            name: ApiClientNames.Basket,
            configureClient: client => client.BaseAddress = baseAddress);

        IHttpClientBuilder ordering = services.AddHttpClient<IOrderingApi, OrderingApi>(
            name: ApiClientNames.Ordering,
            configureClient: client => client.BaseAddress = baseAddress);

        IHttpClientBuilder identity = services.AddHttpClient<IIdentityApi, IdentityApi>(
            name: ApiClientNames.Identity,
            configureClient: client => client.BaseAddress = baseAddress);

        if (configure is null)
        {
            catalog.AddHttpMessageHandler<RequestedWithHandler>();
            basket.AddHttpMessageHandler<RequestedWithHandler>();
            ordering.AddHttpMessageHandler<RequestedWithHandler>();
            identity.AddHttpMessageHandler<RequestedWithHandler>();
        }
        else
        {
            configure.Invoke(obj: catalog);
            configure.Invoke(obj: basket);
            configure.Invoke(obj: ordering);
            configure.Invoke(obj: identity);
        }

        return services;
    }
}
