// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;
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

        return services;
    }
}
