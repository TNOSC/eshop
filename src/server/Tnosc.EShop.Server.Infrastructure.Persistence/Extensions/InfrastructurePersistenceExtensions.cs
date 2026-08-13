// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Infrastructure.Persistence.Basket.Queries;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Repositories;
using Tnosc.Lib.Infrastructure.Persistence.Extensions;
using DomainAssemblyReference = Tnosc.EShop.Server.Domain.AssemblyReference;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Extensions;

/// <summary>
/// Wires <see cref="EShopWriteDbContext"/> and <see cref="EShopReadDbContext"/> to Postgres via
/// Aspire, and registers the shared persistence-layer services from <c>Tnosc.Lib.Infrastructure.Persistence</c>.
/// </summary>
public static class InfrastructurePersistenceExtensions
{
    private const string ConnectionName = "eshopdb";

    /// <summary>
    /// Registers the write and read database contexts against the <c>eshopdb</c> Aspire connection,
    /// then calls <see cref="PersistenceExtensions.AddPersistence{TWriteContext, TReadContext}"/>.
    /// </summary>
    /// <param name="builder">The host application builder to register services on.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    public static IHostApplicationBuilder AddInfrastructurePersistence(this IHostApplicationBuilder builder)
    {
        // The write context opts out of Npgsql's retrying execution strategy. EF Core refuses a
        // user-initiated transaction while one is configured — "does not support user-initiated
        // transactions" — and explicit transactions are load-bearing on this side: TransactionDecorator
        // needs one for [Transactional], and IdempotencyDecorator needs one to commit its claim with
        // the handler's writes. Retry policy is not lost, it is just owned explicitly instead of
        // invisibly: RetryDecorator and [Retry(n)] sit in the command pipeline and can see the
        // Result. The read context keeps the strategy — it never opens a transaction.
        builder.AddNpgsqlDbContext<EShopWriteDbContext>(
            connectionName: ConnectionName,
            configureSettings: static settings => settings.DisableRetry = true);
        builder.AddNpgsqlDbContext<EShopReadDbContext>(connectionName: ConnectionName);

        builder.AddPersistence<EShopWriteDbContext, EShopReadDbContext>(configure: options =>
        {
            options.ConnectionName = ConnectionName;
            options.ConfigurationAssembly = AssemblyReference.Assembly;
            options.DomainEventAssemblies = [DomainAssemblyReference.Assembly];
            options.MigrationsAssembly = AssemblyReference.Assembly;
            options.ApplyMigrationsOnStartup =
                bool.TryParse(value: builder.Configuration["Persistence:ApplyMigrationsOnStartup"], result: out bool applyOnStartup)
                    && applyOnStartup;
        });

        builder.Services.AddRepositories();
        builder.Services.AddQueries();

        // IProductLookup implements no IQueryHandler<,>, so AddQueries' scan misses it — it is a
        // Basket-owned application port, not a CQRS query handler. Registered explicitly here rather
        // than in Server.Infrastructure.External because its implementation queries EShopReadDbContext,
        // a genuine Postgres read; see plan/13-t12-basket.md.
        builder.Services.AddScoped<IProductLookup, ProductLookup>();

        return builder;
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        // Registered as their own domain-facing interface (IProductRepository, …) as well as the
        // closed IRepository<,>, so a domain factory can take the specific contract it needs.
        services.Scan(action: s => s.FromAssemblies(assemblies: AssemblyReference.Assembly)
            .AddClasses(action: c => c.AssignableTo(type: typeof(IRepository<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
    }

    private static void AddQueries(this IServiceCollection services)
    {
        services.Scan(action: s => s.FromAssemblies(assemblies: AssemblyReference.Assembly)
            .AddClasses(action: c => c.AssignableTo(type: typeof(IQueryHandler<,>)), publicOnly: false)
                .As(selector: implementationType => ScanExtensions.ClosedInterfacesOf(implementationType: implementationType, openGenericInterface: typeof(IQueryHandler<,>))).WithScopedLifetime());

        // Queries — innermost first.
        services.TryDecorate(serviceType: typeof(IQueryHandler<,>), decoratorType: typeof(RetryDecorator.QueryHandler<,>));
        services.TryDecorate(serviceType: typeof(IQueryHandler<,>), decoratorType: typeof(CacheableDecorator.QueryHandler<,>));
        services.TryDecorate(serviceType: typeof(IQueryHandler<,>), decoratorType: typeof(ExceptionDecorator.QueryHandler<,>));
        services.TryDecorate(serviceType: typeof(IQueryHandler<,>), decoratorType: typeof(LoggingDecorator.QueryHandler<,>));
    }
}
