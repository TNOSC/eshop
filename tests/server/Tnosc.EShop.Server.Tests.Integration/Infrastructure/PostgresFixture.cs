// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Tnosc.EShop.Server.Application.Extensions;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Extensions;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Host.Extensions;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;
using DomainAssemblyReference = Tnosc.EShop.Server.Domain.AssemblyReference;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure;

/// <summary>
/// Starts a real, reusable Postgres container, wires the SAME registration path production uses —
/// <c>AddApplication</c> + <c>AddInfrastructurePersistence</c> — against it, applies EF Core
/// migrations, and exposes a <see cref="Respawner"/>-backed <see cref="ResetAsync"/> so every
/// integration test starts from an empty, migrated schema.
/// </summary>
/// <remarks>
/// One instance is shared by every test via <see cref="PostgresCollection"/>. xUnit 2.9.3's
/// <see cref="IAsyncLifetime"/> is <see cref="Task"/>-based, not the xUnit v3 <c>ValueTask</c> shape.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime, IAsyncDisposable
{
    private const string ConnectionStringConfigurationKey = "ConnectionStrings:eshopdb";

    private static readonly string[] SchemasToReset = ["catalog", "identity", "basket", "ordering", "payment", "outbox", "idempotency"];

    private PostgreSqlContainer? _container;
    private IHost? _host;
    private NpgsqlConnection? _respawnConnection;
    private Respawner? _respawner;

    /// <summary>
    /// Gets the root service provider built against the container, using the production
    /// registration path. Tests never resolve services from this directly — see
    /// <see cref="IntegrationTestBase"/> for the per-test scope.
    /// </summary>
    public IServiceProvider Services => (_host ?? throw NotInitialized()).Services;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder(image: "postgres:18-alpine")
            .WithReuse(reuse: true)
            .Build();

        await _container.StartAsync();

        string connectionString = _container.GetConnectionString();

        _host = BuildHost(connectionString: connectionString);

        using (IServiceScope scope = _host.Services.CreateScope())
        {
            EShopWriteDbContext writeContext = scope.ServiceProvider.GetRequiredService<EShopWriteDbContext>();
            await writeContext.Database.MigrateAsync();
            await CreateTestModelTableAsync(writeContext: writeContext);
        }

        _respawnConnection = new NpgsqlConnection(connectionString: connectionString);
        await _respawnConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection: _respawnConnection, options: new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = SchemasToReset,
            TablesToIgnore = [new Table(name: "__EFMigrationsHistory")]
        });
    }

    /// <summary>
    /// Builds the application host using the SAME registration path production uses —
    /// <c>AddApplication</c> + <c>AddInfrastructurePersistence</c> — pointed at
    /// <paramref name="connectionString"/>, plus the test-only overrides <see cref="IntegrationTestBase"/>
    /// and the outbox tests depend on: a deterministic <see cref="TimeProvider"/>, the delivery spy,
    /// the extended write model, and the widened domain-event registry.
    /// </summary>
    /// <param name="connectionString">The Postgres connection string to register under <c>eshopdb</c>.</param>
    /// <returns>The built <see cref="IHost"/>.</returns>
    private static IHost BuildHost(string connectionString)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration[ConnectionStringConfigurationKey] = connectionString;

        // Deterministic clock for backoff / audit-stamping assertions. Registered before
        // AddInfrastructurePersistence so its TryAddSingleton(TimeProvider.System) becomes a no-op.
        builder.Services.AddSingleton<TestTimeProvider>();
        builder.Services.AddSingleton<TimeProvider>(implementationFactory: sp => sp.GetRequiredService<TestTimeProvider>());

        builder.Services.AddSingleton<TestDomainEventSpy>();

        // AddApplication only scans the Server.Application assembly for IDomainEventHandler<>
        // implementors, so the test-only handlers need registering by hand.
        builder.Services.AddScoped<IDomainEventHandler<TestAggregateCreatedDomainEvent>, TestDomainEventHandler>();
        builder.Services.AddScoped<IDomainEventHandler<PoisonTestDomainEvent>, PoisonTestDomainEventHandler>();

        builder.Services.AddUserContext();

        // The query pipeline's CacheableDecorator takes a HybridCache, so resolving any
        // IQueryHandler<,> needs one registered — exactly as Program.cs does for the real host.
        builder.Services.AddHybridCache();

        builder.Services.AddApplication();
        builder.AddInfrastructurePersistence();

        // Extend the write model with the test-only aggregate without touching the sealed,
        // production EShopWriteDbContext — see TestModelCustomizer's remarks for why.
        ReplaceModelCustomizerFor(services: builder.Services);

        // The production registry only scans the Server.Domain assembly. Add this test assembly so
        // the outbox processor can resolve and deserialize the test-only domain events.
        builder.Services.Replace(descriptor: ServiceDescriptor.Singleton<IDomainEventTypeRegistry>(
            implementationFactory: _ => new DomainEventTypeRegistry(DomainAssemblyReference.Assembly, typeof(TestAggregateCreatedDomainEvent).Assembly)));

        return builder.Build();
    }

    /// <summary>
    /// Creates <see cref="TestAggregateConfiguration"/>'s physical table by hand, once, idempotently.
    /// No EF migration describes it — <see cref="TestModelCustomizer"/> only extends the runtime
    /// model, it never touches the migration files under source control.
    /// </summary>
    /// <param name="writeContext">The write context to run the raw SQL against.</param>
    private static async Task CreateTestModelTableAsync(EShopWriteDbContext writeContext) =>
        await writeContext.Database.ExecuteSqlRawAsync(sql: $"""
            CREATE SCHEMA IF NOT EXISTS {TestAggregateConfiguration.SchemaName};
            CREATE TABLE IF NOT EXISTS {TestAggregateConfiguration.SchemaName}.{TestAggregateConfiguration.TableName} (
                id uuid PRIMARY KEY,
                name character varying(200) NOT NULL,
                created_on_utc timestamp with time zone NOT NULL,
                updated_on_utc timestamp with time zone NOT NULL,
                created_by character varying(200) NOT NULL,
                updated_by character varying(200) NULL,
                version integer NOT NULL
            );
            """);

    /// <inheritdoc cref="IAsyncLifetime.DisposeAsync" />
    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_respawnConnection is not null)
        {
            await _respawnConnection.DisposeAsync();
        }

        _host?.Dispose();

        if (_container is not null)
        {
            // WithReuse(true) means Testcontainers deliberately leaves the container running for the
            // next test run rather than stopping it here.
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Truncates every table in the schemas under test back to empty. Called by
    /// <see cref="IntegrationTestBase"/> before every test so tests never observe another test's data.
    /// </summary>
    public async Task ResetAsync() =>
        await (_respawner ?? throw NotInitialized()).ResetAsync(connection: _respawnConnection ?? throw NotInitialized());

    /// <summary>
    /// Replaces <c>DbContextOptions&lt;EShopWriteDbContext&gt;</c>'s registration with one that wraps
    /// the original factory and additionally calls
    /// <see cref="DbContextOptionsBuilder.ReplaceService{TService, TImplementation}"/> to swap in
    /// <see cref="TestModelCustomizer"/>. This is done by decorating the existing registration rather
    /// than registering a second, competing one, since EF Core's own registration already won the
    /// <c>TryAdd</c> race inside <c>AddInfrastructurePersistence</c>.
    /// </summary>
    /// <param name="services">The service collection built by <c>AddInfrastructurePersistence</c>.</param>
    private static void ReplaceModelCustomizerFor(IServiceCollection services)
    {
        ServiceDescriptor original = services.Single(predicate: descriptor => descriptor.ServiceType == typeof(DbContextOptions<EShopWriteDbContext>));
        Func<IServiceProvider, object> originalFactory = original.ImplementationFactory
            ?? throw new InvalidOperationException(message: "DbContextOptions<EShopWriteDbContext> was not registered via a factory.");

        services.Remove(item: original);
        services.Add(item: ServiceDescriptor.Describe(
            serviceType: typeof(DbContextOptions<EShopWriteDbContext>),
            implementationFactory: serviceProvider =>
            {
                var options = (DbContextOptions<EShopWriteDbContext>)originalFactory(serviceProvider);
                return new DbContextOptionsBuilder<EShopWriteDbContext>(options: options)
                    .ReplaceService<IModelCustomizer, TestModelCustomizer>()
                    // TestAggregate deliberately diverges the runtime model from the last migration's
                    // snapshot — EF's own startup check for that is irrelevant here and would otherwise
                    // fail MigrateAsync.
                    .ConfigureWarnings(warningsConfigurationBuilderAction: warnings => warnings.Ignore(eventIds: RelationalEventId.PendingModelChangesWarning))
                    .Options;
            },
            lifetime: original.Lifetime));
    }

    private static InvalidOperationException NotInitialized() =>
        new("PostgresFixture has not been initialized. InitializeAsync must run before use.");
}
