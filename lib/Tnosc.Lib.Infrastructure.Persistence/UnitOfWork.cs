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
public sealed class UnitOfWork<TContext> : IUnitOfWork, IAsyncDisposable
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IUserContext _userContext;
    private readonly IDomainEventTypeRegistry _domainEventTypeRegistry;
    private readonly TimeProvider _timeProvider;
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork{TContext}"/> class.
    /// </summary>
    /// <param name="context">The <typeparamref name="TContext"/> <see cref="DbContext"/> instance to wrap.</param>
    /// <param name="userContext">Provides information about the current caller.</param>
    /// <param name="domainEventTypeRegistry">Resolves the durable contract name for a domain event type.</param>
    /// <param name="timeProvider">Supplies the current UTC time for audit stamping.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the required parameters is <c>null</c>.
    /// </exception>
    public UnitOfWork(
        TContext context,
        IUserContext userContext,
        IDomainEventTypeRegistry domainEventTypeRegistry,
        TimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(paramName: nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(paramName: nameof(userContext));
        _domainEventTypeRegistry = domainEventTypeRegistry ?? throw new ArgumentNullException(paramName: nameof(domainEventTypeRegistry));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(paramName: nameof(timeProvider));
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
            return await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        }
        catch (DBConcurrencyException ex)
        {
            // throw conflict exception to be handled by upper layers
            throw new ConflictException(message: "A concurrency conflict occurred while saving changes to the database.", correlationId: null, inner: ex);
        }
    }

    /// <summary>
    /// Begins a new transaction on the underlying database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when a transaction is already in flight.</exception>
    public async ValueTask BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            throw new InvalidOperationException(message: "A transaction is already in progress on this unit of work.");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken: cancellationToken);
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
            await _currentTransaction.CommitAsync(cancellationToken: cancellationToken);
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
            await _currentTransaction.RollbackAsync(cancellationToken: cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Releases the transaction currently in flight, if any.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    private void ConvertDomainEventsToOutboxMessage()
    {
        var outboxMessages = _context.ChangeTracker
            .Entries<IHasDomainEvent>()
            .Select(selector: x => x.Entity)
            .SelectMany(selector: aggregateRoot =>
            {
                List<IDomainEvent> events = [.. aggregateRoot.DomainEvents];
                aggregateRoot.ClearDomainEvents();

                return events;

            })
            .Select(selector: domainEvent => new OutboxMessage(
                type: _domainEventTypeRegistry.GetName(domainEventType: domainEvent.GetType()),
                content: JsonSerializer.Serialize(value: domainEvent, inputType: domainEvent.GetType(), options: OutboxSerialization.Options)
            ))
            .ToList();

        _context.Set<OutboxMessage>().AddRange(entities: outboxMessages);
    }

    private void UpdateAuditableEntries()
    {
        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (EntityEntry<IAuditable> auditable in _context.ChangeTracker.Entries<IAuditable>())
        {
            if (auditable.State == EntityState.Added)
            {
                auditable.Property(propertyName: nameof(IAuditable.CreatedOnUtc)).CurrentValue = utcNow;
                auditable.Property(propertyName: nameof(IAuditable.CreatedBy)).CurrentValue = _userContext.UserId ?? "system";
            }

            if (auditable.State == EntityState.Modified)
            {
                auditable.Property(propertyName: nameof(IAuditable.UpdatedOnUtc)).CurrentValue = utcNow;
                auditable.Property(propertyName: nameof(IAuditable.UpdatedBy)).CurrentValue = _userContext.UserId ?? "system";
            }
        }
    }
}
