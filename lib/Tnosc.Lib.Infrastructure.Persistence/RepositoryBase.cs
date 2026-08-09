// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence;

/// <summary>
/// Provides a base implementation for a repository that manages aggregate root entities using Entity Framework Core.
/// </summary>
/// <remarks>This abstract class defines common data access operations for aggregate roots, such as retrieval,
/// addition, update, and removal. It is intended to be inherited by concrete repository implementations that interact
/// with a specific <see cref="DbContext"/>. All operations are performed using Entity Framework Core's <see
/// cref="DbSet{TEntity}"/>.</remarks>
/// <typeparam name="TAggregateRoot">The type of the aggregate root entity managed by the repository. Must implement <see cref="IAggregateRoot"/>.</typeparam>
/// <typeparam name="TId">The type of the unique identifier for the aggregate root entity.</typeparam>
public abstract class RepositoryBase<TAggregateRoot, TId>
    where TAggregateRoot : class, IAggregateRoot
{
    private readonly DbSet<TAggregateRoot> _dbSet;

    /// <summary>
    /// Initializes a new instance of the RepositoryBase class using the specified database context.
    /// </summary>
    /// <param name="context">The DbContext instance to be used for data access operations. Cannot be null.</param>
    protected RepositoryBase(DbContext context) =>
        _dbSet = context.Set<TAggregateRoot>();

    /// <summary>
    /// Asynchronously retrieves an aggregate root entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the aggregate root to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the aggregate root entity if found;
    /// otherwise, <see langword="null"/>.</returns>
    public async ValueTask<TAggregateRoot?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await _dbSet.FindAsync([id], cancellationToken);

    /// <summary>
    /// Asynchronously adds the specified aggregate root entity to the context.
    /// </summary>
    /// <param name="entity">The aggregate root entity to add. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A ValueTask that represents the asynchronous add operation.</returns>
    public async ValueTask AddAsync(TAggregateRoot entity, CancellationToken cancellationToken = default) =>
        await _dbSet.AddAsync(entity, cancellationToken);

    /// <summary>
    /// Updates the specified aggregate root entity in the underlying data store.
    /// </summary>
    /// <param name="entity">The aggregate root entity to update. Cannot be null.</param>
    public void Update(TAggregateRoot entity) =>
        _dbSet.Update(entity);

    /// <summary>
    /// Removes the specified aggregate root entity from the context, marking it for deletion in the underlying data
    /// store.
    /// </summary>
    /// <remarks>The entity will be deleted from the data store when changes are saved to the context. If the
    /// entity is not being tracked, this method has no effect.</remarks>
    /// <param name="entity">The aggregate root entity to remove. Cannot be null.</param>
    public void Remove(TAggregateRoot entity) =>
        _dbSet.Remove(entity);
}
