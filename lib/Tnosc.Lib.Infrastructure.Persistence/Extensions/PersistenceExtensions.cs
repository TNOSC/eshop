// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Migrations;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Tnosc.Lib.Infrastructure.Persistence.Publishers;

namespace Tnosc.Lib.Infrastructure.Persistence.Extensions;

/// <summary>
/// Registers the persistence-layer services shared by every bounded context: the unit of work, the
/// domain-event type registry and publisher, the outbox processor, and the two startup hosted
/// services that apply migrations and drain the outbox.
/// </summary>
/// <remarks>
/// This method deliberately does <b>not</b> register <c>TWriteContext</c> or <c>TReadContext</c>
/// themselves. Doing so needs Aspire's <c>AddNpgsqlDbContext</c>, which pulls in the Npgsql package —
/// and <c>lib/</c> stays Npgsql-free by design (the outbox claim query and the migration advisory
/// lock are the only Postgres-specific code, and both are contained, documented exceptions inside
/// this assembly, not a package dependency). The composition root must call
/// <c>builder.AddNpgsqlDbContext&lt;TWriteContext&gt;(options.ConnectionName)</c> and the read-context
/// equivalent itself — before or after calling <see cref="AddPersistence{TWriteContext, TReadContext}"/>,
/// registration order does not matter as long as both happen before the host is built.
/// </remarks>
public static class PersistenceExtensions
{
    /// <summary>
    /// Registers the persistence-layer services described in the type remarks.
    /// </summary>
    /// <typeparam name="TWriteContext">The write-side <see cref="WriteDbContextBase"/> for this host.</typeparam>
    /// <typeparam name="TReadContext">The read-side <see cref="ReadDbContextBase"/> for this host.</typeparam>
    /// <param name="builder">The host application builder to register services on.</param>
    /// <param name="configure">Configures the <see cref="PersistenceOptions"/> for this registration.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="configure"/> left <see cref="PersistenceOptions.ConnectionName"/>,
    /// <see cref="PersistenceOptions.ConfigurationAssembly"/>, or <see cref="PersistenceOptions.DomainEventAssemblies"/> unset.
    /// </exception>
    public static IHostApplicationBuilder AddPersistence<TWriteContext, TReadContext>(
        this IHostApplicationBuilder builder,
        Action<PersistenceOptions> configure)
        where TWriteContext : WriteDbContextBase
        where TReadContext : ReadDbContextBase
    {
        ArgumentNullException.ThrowIfNull(argument: builder);
        ArgumentNullException.ThrowIfNull(argument: configure);

        var options = new PersistenceOptions();
        configure(options);
        Validate(options: options);

        builder.Services.TryAddSingleton(instance: TimeProvider.System);

        builder.Services.TryAddSingleton<IDomainEventTypeRegistry>(
            implementationFactory: _ => new DomainEventTypeRegistry(assemblies: options.DomainEventAssemblies));

        // Singleton over the ROOT provider: DomainEventsPublisher opens a fresh scope per event so
        // handler-scoped services (including their own DbContext) stay isolated from the processor's
        // context. Registering it as anything other than a singleton would break that isolation.
        builder.Services.TryAddSingleton<IDomainEventsPublisher, DomainEventsPublisher>();

        builder.Services.AddScoped<IUnitOfWork, UnitOfWork<TWriteContext>>();

        builder.Services.Configure<OutboxOptions>(configureOptions: o =>
        {
            o.BatchSize = options.Outbox.BatchSize;
            o.PollingInterval = options.Outbox.PollingInterval;
            o.MaxAttempts = options.Outbox.MaxAttempts;
            o.BaseBackoff = options.Outbox.BaseBackoff;
        });

        builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor<TWriteContext>>();

        builder.Services.Configure<IdempotencyOptions>(configureOptions: o =>
        {
            o.Retention = options.Idempotency.Retention;
            o.CleanupInterval = options.Idempotency.CleanupInterval;
            o.BatchSize = options.Idempotency.BatchSize;
        });

        // Scoped, and over the same TWriteContext the handler writes through, so a claim joins
        // whichever transaction the handler is already in rather than opening its own.
        builder.Services.AddScoped<IIdempotencyStore, IdempotencyStore<TWriteContext>>();
        builder.Services.AddScoped<IInboxStore, InboxStore<TWriteContext>>();

        if (options.EnableIdempotencyCleanup)
        {
            builder.Services.AddHostedService<IdempotencyCleanupBackgroundService<TWriteContext>>();
        }

        if (options.ApplyMigrationsOnStartup)
        {
            builder.Services.AddHostedService<DatabaseMigrationHostedService<TWriteContext>>();
        }

        if (options.EnableOutboxProcessing)
        {
            builder.Services.AddHostedService<OutboxBackgroundService<TWriteContext>>();
        }

        return builder;
    }

    private static void Validate(PersistenceOptions options)
    {
        if (string.IsNullOrWhiteSpace(value: options.ConnectionName))
        {
            throw new InvalidOperationException(
                message: $"{nameof(PersistenceOptions.ConnectionName)} must be set by the configuration delegate passed to AddPersistence.");
        }

        if (options.DomainEventAssemblies.Length == 0)
        {
            throw new InvalidOperationException(
                message: $"{nameof(PersistenceOptions.DomainEventAssemblies)} must be set by the configuration delegate passed to AddPersistence.");
        }
    }
}
