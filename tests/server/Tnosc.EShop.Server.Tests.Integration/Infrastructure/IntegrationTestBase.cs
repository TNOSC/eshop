// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for tests running against a real, migrated Postgres instance. Resets every table
/// before each test via <see cref="PostgresFixture.ResetAsync"/>, then opens a fresh
/// <see cref="IServiceScope"/> so every test gets its own <see cref="EShopWriteDbContext"/> and
/// <see cref="EShopReadDbContext"/> instance.
/// </summary>
/// <param name="fixture">The shared Postgres fixture, injected by <see cref="PostgresCollection"/>.</param>
[Collection(nameof(PostgresCollection))]
public abstract class IntegrationTestBase(PostgresFixture fixture) : IAsyncLifetime
{
    private AsyncServiceScope? _scope;

    /// <summary>
    /// Gets the shared fixture, so tests that need more than one <see cref="IServiceScope"/> at once —
    /// the SKIP LOCKED concurrency test, most notably — can open additional scopes of their own.
    /// </summary>
    protected PostgresFixture Fixture => fixture;

    /// <summary>
    /// Gets the per-test <see cref="IServiceScope"/>, opened fresh in <see cref="InitializeAsync"/>.
    /// </summary>
    protected IServiceScope Scope => _scope ?? throw NotInitialized();

    /// <summary>
    /// Gets the scoped write-side <see cref="EShopWriteDbContext"/> for this test.
    /// </summary>
    protected EShopWriteDbContext WriteContext => Scope.ServiceProvider.GetRequiredService<EShopWriteDbContext>();

    /// <summary>
    /// Gets the scoped read-side <see cref="EShopReadDbContext"/> for this test.
    /// </summary>
    protected EShopReadDbContext ReadContext => Scope.ServiceProvider.GetRequiredService<EShopReadDbContext>();

    /// <summary>
    /// Gets the scoped <see cref="IUnitOfWork"/> for this test — audit stamping and domain-event-to-
    /// outbox conversion only happen when saving through this, not through <see cref="WriteContext"/> directly.
    /// </summary>
    protected IUnitOfWork UnitOfWork => Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    /// <summary>
    /// Gets the scoped <see cref="IOutboxProcessor"/> for this test.
    /// </summary>
    protected IOutboxProcessor OutboxProcessor => Scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

    /// <summary>
    /// Gets the manually-advanced clock backing <see cref="TimeProvider"/> for this test run, reset
    /// to the current wall-clock time before every test.
    /// </summary>
    protected TestTimeProvider TimeProvider => Scope.ServiceProvider.GetRequiredService<TestTimeProvider>();

    /// <summary>
    /// Gets the process-wide domain-event delivery spy, cleared before every test.
    /// </summary>
    protected TestDomainEventSpy Spy => Scope.ServiceProvider.GetRequiredService<TestDomainEventSpy>();

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();

        // CreateAsyncScope, not CreateScope: UnitOfWork<TContext> only implements IAsyncDisposable,
        // and IServiceScope.Dispose() throws for a scoped service that has no synchronous Dispose.
        _scope = fixture.Services.CreateAsyncScope();
        Spy.Clear();
        TimeProvider.SetUtcNow(utcNow: DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_scope is { } scope)
        {
            await scope.DisposeAsync();
        }
    }

    /// <summary>
    /// Adds the supplied entities and saves them directly through <see cref="WriteContext"/>, bypassing
    /// <see cref="IUnitOfWork"/> — no audit stamping, no domain-event-to-outbox conversion. Use this to
    /// seed rows (for example, <see cref="OutboxMessage"/> instances in a specific pre-set state) whose
    /// shape a test wants full control over; use <see cref="UnitOfWork"/> directly when a test wants
    /// the real save pipeline.
    /// </summary>
    /// <param name="entities">The entities to add and save.</param>
    protected async Task SeedAsync(params object[] entities)
    {
        WriteContext.AddRange(entities: entities);
        await WriteContext.SaveChangesAsync();

        // Production never processes an outbox row through the same DbContext instance that inserted
        // it — the write and the poll happen in different scopes. Clearing the tracker after seeding
        // reproduces that: the next query against WriteContext re-materializes fresh rows from the
        // database instead of EF silently returning the still-tracked, pre-claim in-memory instance.
        WriteContext.ChangeTracker.Clear();
    }

    private static InvalidOperationException NotInitialized() =>
        new("The test scope has not been initialized. InitializeAsync must run before use.");
}
