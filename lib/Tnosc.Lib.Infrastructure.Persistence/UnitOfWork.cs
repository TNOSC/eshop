// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Exceptions;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;

namespace Tnosc.Lib.Infrastructure.Persistence;

/// <summary>
/// Provides a unit-of-work wrapper around a DbContext to manage transactions and save operations.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class UnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IUserContext _userContext;
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork{TContext}"/> class.
    /// </summary>
    /// <param name="context">The <typeparamref name="TContext"/> <see cref="DbContext"/> instance to wrap.</param>
    /// <param name="userContext">Provides information about the current caller.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <c>null</c>.</exception>
    public UnitOfWork(TContext context, IUserContext userContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Persists all changes made in the current context to the underlying database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public async ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ConvertDomainEventsToOutboxMessage();
            UpdateAuditableEntries();
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DBConcurrencyException ex)
        {
            // throw conflict exception to be handled by upper layers
            throw new ConflictException("A concurrency conflict occurred while saving changes to the database.", correlationId: null, inner: ex);
        }
    }

    /// <summary>
    /// Begins a new transaction on the underlying database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction, persisting its changes to the underlying database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction, discarding its changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    private void ConvertDomainEventsToOutboxMessage()
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        IEnumerable<OutboxMessage> outboxMessages = _context.ChangeTracker
            .Entries<IHasDomainEvent>()
            .Select(x => x.Entity)
            .SelectMany(aggregateRoot =>
            {
                IReadOnlyCollection<IDomainEvent> events = aggregateRoot.DomainEvents;
                aggregateRoot.ClearDomainEvents();

                return events;

            })
            .Select(domainEvent => new OutboxMessage(
                domainEvent.GetType().Name,
                JsonSerializer.Serialize(domainEvent, jsonSerializerOptions)
            ));

        _context.Set<OutboxMessage>().AddRange(outboxMessages);
    }

    private void UpdateAuditableEntries()
    {
        foreach (EntityEntry<IAuditable> auditable in _context.ChangeTracker.Entries<IAuditable>())
        {
            if (auditable.State == EntityState.Added)
            {
                auditable.Property(nameof(IAuditable.CreatedOnUtc)).CurrentValue = DateTimeOffset.UtcNow;
                auditable.Property(nameof(IAuditable.CreatedBy)).CurrentValue = _userContext.UserId ?? "system";
            }

            if (auditable.State == EntityState.Modified)
            {
                auditable.Property(nameof(IAuditable.UpdatedOnUtc)).CurrentValue = DateTimeOffset.UtcNow;
                auditable.Property(nameof(IAuditable.UpdatedBy)).CurrentValue = _userContext.UserId ?? "system";
            }
        }
    }
}
