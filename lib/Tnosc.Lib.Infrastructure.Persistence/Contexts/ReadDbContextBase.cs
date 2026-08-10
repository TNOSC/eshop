// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Tnosc.Lib.Infrastructure.Persistence.Contexts;

/// <summary>
/// Base type for the query-side <see cref="DbContext"/> of a bounded context, mapping dedicated
/// read-model classes rather than domain aggregates, and forbidding writes.
/// </summary>
public abstract class ReadDbContextBase : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadDbContextBase"/> class and configures the
    /// change tracker for read-only, no-tracking use.
    /// </summary>
    /// <param name="options">The EF Core options for this context.</param>
    /// <remarks>
    /// Tracking behaviour is set on the <see cref="DbContext.ChangeTracker"/> here, in the constructor,
    /// rather than through <see cref="DbContextOptions"/>, so it holds regardless of how dependency
    /// injection built those options.
    /// </remarks>
#pragma warning disable MA0056 // ChangeTracker must be configured here, in the constructor, so the
                               // read-only guarantee holds regardless of how DI built `options`.
    protected ReadDbContextBase(DbContextOptions options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
        ChangeTracker.LazyLoadingEnabled = false;
    }
#pragma warning restore MA0056

    /// <summary>
    /// Gets the assembly scanned for this context's read-model <see cref="IEntityTypeConfiguration{TEntity}"/> classes.
    /// </summary>
    protected abstract Assembly ConfigurationAssembly { get; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelComposition.ApplyReadModel(modelBuilder: modelBuilder, assembly: ConfigurationAssembly);
        base.OnModelCreating(modelBuilder: modelBuilder);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ModelComposition.ApplySharedConventions(configurationBuilder: configurationBuilder, assembly: ConfigurationAssembly);
        base.ConfigureConventions(configurationBuilder: configurationBuilder);
    }

    /// <summary>
    /// Always throws — the read context is read-only.
    /// </summary>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public sealed override int SaveChanges() => throw ReadOnly();

    /// <summary>
    /// Always throws — the read context is read-only.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Ignored.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess) => throw ReadOnly();

    /// <summary>
    /// Always throws — the read context is read-only.
    /// </summary>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw ReadOnly();

    /// <summary>
    /// Always throws — the read context is read-only.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Ignored.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public sealed override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) => throw ReadOnly();

    private static NotSupportedException ReadOnly() =>
        new("The read context is read-only. Use the write context and IUnitOfWork for mutations.");
}
