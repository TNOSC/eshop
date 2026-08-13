// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Application.Payment.Ports;
using Tnosc.EShop.Server.Infrastructure.External.Payment;
using Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;
using Tnosc.EShop.Server.Infrastructure.External.Redis.Ordering;
using BasketRepositoryContract = Tnosc.EShop.Server.Domain.Basket.Baskets.IBasketRepository;

namespace Tnosc.EShop.Server.Infrastructure.External.Extensions;

/// <summary>
/// Composition root for <c>Server.Infrastructure.External</c> — everything that talks to a system
/// outside this process over a client the way EF Core is outside <c>Server.Infrastructure.Persistence</c>.
/// Basket's Redis store is the first tenant.
/// </summary>
public static class ExternalExtensions
{
    /// <summary>
    /// Registers the External layer's services: the Redis-backed <c>IBasketRepository</c> and
    /// <c>IBasketReader</c>, and <see cref="BasketOptions"/> bound from configuration.
    /// </summary>
    /// <remarks>
    /// Neither Redis-backed type implements <c>IRepository&lt;,&gt;</c> or <c>IQueryHandler&lt;,&gt;</c>,
    /// so neither is picked up by a Scrutor assembly scan — both are registered explicitly here. This
    /// is also the one place in this project that touches <see cref="IConfiguration"/> or
    /// <see cref="IOptions{TOptions}"/>, per <c>.claude/rules/configuration-options.md</c>; every
    /// consumer takes the plain <see cref="BasketOptions"/> class instead.
    /// </remarks>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configuration">The configuration to bind <see cref="BasketOptions"/> from.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddInfrastructureExternal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BasketOptions>()
            .Bind(config: configuration.GetSection(key: BasketOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(implementationFactory: resolve => resolve.GetRequiredService<IOptions<BasketOptions>>().Value);

        services.AddScoped<BasketRepositoryContract, RedisBasketRepository>();
        services.AddScoped<IBasketReader, RedisBasketReader>();

        // Ordering's view of the same store. An adapter over IBasketReader rather than a second Redis
        // client, so the key format and document schema stay defined once — see RedisOrderBasketReader.
        services.AddScoped<IOrderBasketReader, RedisOrderBasketReader>();

        // Payment's external boundary. Registered as a typed HttpClient with the standard resilience
        // handler (retry, circuit breaker, timeout) per plan/15-t14-payment.md — see FakePaymentGateway
        // for why nothing here issues a real call yet.
        services.AddHttpClient<IPaymentGateway, FakePaymentGateway>()
            .AddStandardResilienceHandler();

        return services;
    }
}
