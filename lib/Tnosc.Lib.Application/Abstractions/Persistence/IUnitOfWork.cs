// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.Lib.Application.Abstractions.Persistence;

/// <summary>
/// Represents a unit of work abstraction for persistence operations.
/// Provides methods to save changes and to begin a database transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets a value indicating whether a transaction is currently in flight on this unit of work.
    /// </summary>
    /// <remarks>
    /// Lets a decorator that needs a transaction join one an outer decorator already opened instead
    /// of nesting — <see cref="BeginTransactionAsync"/> throws on a nested begin. Needed because
    /// <c>IdempotencyDecorator</c> runs inside <c>TransactionDecorator</c> and must not assume
    /// which of the two owns the transaction.
    /// </remarks>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Persists all changes made in this unit of work to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The number of state entries written to the underlying database.</returns>
    ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new transaction on the underlying store.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    ValueTask BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction, persisting its changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    ValueTask CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction, discarding its changes.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    ValueTask RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
