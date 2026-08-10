// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tnosc.Lib.Infrastructure.Persistence.Migrations;

/// <summary>
/// Applies pending EF Core migrations for <typeparamref name="TContext"/> during host startup.
/// </summary>
/// <remarks>
/// Implements <see cref="IHostedService"/> rather than <see cref="BackgroundService"/> so
/// <see cref="StartAsync"/> — and therefore the migration — completes before the host finishes
/// starting and begins serving traffic. A Postgres advisory lock serializes concurrent replicas so
/// they never race EF Core's migration history table; like the outbox claim query, this is a
/// deliberate, documented Postgres-specific exception contained to this class.
/// </remarks>
/// <typeparam name="TContext">The <see cref="DbContext"/> whose migrations should be applied.</typeparam>
public sealed class DatabaseMigrationHostedService<TContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseMigrationHostedService<TContext>> logger) : IHostedService
    where TContext : DbContext
{
    // Arbitrary, stable key identifying this migration's advisory lock. Any int64 works as long as
    // it stays constant across deployments and doesn't collide with another advisory lock in use.
    private const long AdvisoryLockKey = 851_972_301;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        TContext context = scope.ServiceProvider.GetRequiredService<TContext>();

        await context.Database.ExecuteSqlInterpolatedAsync(sql: $"SELECT pg_advisory_lock({AdvisoryLockKey})", cancellationToken: cancellationToken);

        try
        {
            logger.LogInformation(message: "Applying pending migrations for {ContextType}.", args: typeof(TContext).Name);
            await context.Database.MigrateAsync(cancellationToken: cancellationToken);
        }
        finally
        {
            await context.Database.ExecuteSqlInterpolatedAsync(sql: $"SELECT pg_advisory_unlock({AdvisoryLockKey})", cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
