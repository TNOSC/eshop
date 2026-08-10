// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Extensions;
using Tnosc.Lib.Application.Queries;
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
        builder.AddNpgsqlDbContext<EShopWriteDbContext>(connectionName: ConnectionName);
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

        builder.Services.AddQueries();

        return builder;
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
