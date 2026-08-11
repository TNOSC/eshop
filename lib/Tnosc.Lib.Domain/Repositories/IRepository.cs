// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.Lib.Domain.Repositories;

/// <summary>
/// Defines the command-side persistence contract for an aggregate root.
/// </summary>
/// <remarks>
/// This contract lives in the domain layer rather than the application layer. Uniqueness
/// invariants (for example, "no two products share a SKU") are business invariants, and a domain
/// factory enforcing them must be able to query a repository; placing the contract in the
/// application layer would force that check back into the handler as a business <c>if</c>, which
/// the architecture rules forbid. <c>IUnitOfWork</c> stays in the application layer — committing a
/// transaction is an orchestration concern, not a domain one.
/// </remarks>
/// <typeparam name="TAggregateRoot">The type of the aggregate root managed by the repository.</typeparam>
/// <typeparam name="TEntityId">The type of the aggregate root's strongly-typed identifier.</typeparam>
public interface IRepository<TAggregateRoot, in TEntityId>
    where TAggregateRoot : class, IAggregateRoot<TEntityId>
    where TEntityId : class, IEntityId
{
    /// <summary>
    /// Retrieves the aggregate root with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the aggregate root to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching aggregate root, or <see langword="null"/> if none exists.</returns>
    ValueTask<TAggregateRoot?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new aggregate root to the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate root to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    ValueTask AddAsync(TAggregateRoot aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing aggregate root as updated.
    /// </summary>
    /// <param name="aggregate">The aggregate root to update.</param>
    void Update(TAggregateRoot aggregate);

    /// <summary>
    /// Marks an existing aggregate root for removal.
    /// </summary>
    /// <param name="aggregate">The aggregate root to remove.</param>
    void Remove(TAggregateRoot aggregate);
}
