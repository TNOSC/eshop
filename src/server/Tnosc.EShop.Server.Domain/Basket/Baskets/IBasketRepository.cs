// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// Persistence contract for <see cref="Basket"/>.
/// </summary>
/// <remarks>
/// Deliberately does <b>not</b> extend <c>IRepository&lt;Basket, BasketId&gt;</c>. That contract's
/// <c>AddAsync</c>/<c>Update</c>/<c>Remove</c> are unit-of-work-shaped — they stage work for a later
/// <c>SaveChangesAsync</c>. Redis has no such deferral: every write here is immediate, so pretending
/// otherwise would leave <c>Update</c> looking like it did nothing. Because Scrutor's
/// <c>AddRepositories()</c> scans the persistence assembly for <c>IRepository&lt;,&gt;</c>
/// implementations, an implementation of this narrower contract is registered explicitly instead —
/// see <c>ExternalExtensions.AddInfrastructureExternal</c>.
/// </remarks>
public interface IBasketRepository
{
    /// <summary>
    /// Retrieves the basket belonging to the specified customer.
    /// </summary>
    /// <param name="customerId">The identifier of the customer whose basket to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The customer's basket, or <see langword="null"/> if they have none.</returns>
    ValueTask<Basket?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a basket immediately, using optimistic concurrency on <c>Version</c>.
    /// </summary>
    /// <param name="basket">The basket to persist.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="Tnosc.Lib.Application.Exceptions.ConflictException">
    /// The stored basket's version no longer matches what <paramref name="basket"/> was loaded from —
    /// another write landed first.
    /// </exception>
    ValueTask SaveAsync(Basket basket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified customer's basket entirely.
    /// </summary>
    /// <param name="customerId">The identifier of the customer whose basket to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    ValueTask RemoveAsync(Guid customerId, CancellationToken cancellationToken = default);
}
