// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;

namespace Tnosc.Lib.Infrastructure.Persistence.Contexts;

/// <summary>
/// Base type for the command-side <see cref="DbContext"/> of a bounded context, mapping domain
/// aggregates through their <see cref="IEntityTypeConfiguration{TEntity}"/> classes plus the outbox.
/// </summary>
/// <remarks>
/// Phase-2 split path: today <c>TransactionDecorator</c> injects a single unkeyed <c>IUnitOfWork</c>,
/// and the outbox insert must share the aggregate's transaction — so one write context per process is
/// the only coherent option. Splitting write contexts per bounded context later requires registering
/// each write context as a keyed service and changing the transaction decorator to resolve
/// <c>[FromKeyedServices(...)] IUnitOfWork</c> instead of a single unkeyed one.
/// </remarks>
/// <param name="options">The EF Core options for this context.</param>
public abstract class WriteDbContextBase(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets the assembly scanned for this context's <see cref="IEntityTypeConfiguration{TEntity}"/> classes.
    /// </summary>
    protected abstract Assembly ConfigurationAssembly { get; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelComposition.ApplyWriteModel(modelBuilder: modelBuilder, assembly: ConfigurationAssembly);

        // Applied explicitly, not by the scan: ApplyWriteModel only picks up configurations whose
        // entity implements IAggregateRoot, and none of these three is a domain aggregate.
        modelBuilder.ApplyConfiguration(configuration: new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(configuration: new IdempotencyRequestConfiguration());
        modelBuilder.ApplyConfiguration(configuration: new ProcessedEventConfiguration());
        base.OnModelCreating(modelBuilder: modelBuilder);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ModelComposition.ApplySharedConventions(configurationBuilder: configurationBuilder, assembly: ConfigurationAssembly);
        base.ConfigureConventions(configurationBuilder: configurationBuilder);
    }
}
