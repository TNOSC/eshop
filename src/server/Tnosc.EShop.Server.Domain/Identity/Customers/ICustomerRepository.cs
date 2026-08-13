// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain.Repositories;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Command-side persistence contract for <see cref="Customer"/>.
/// </summary>
/// <remarks>
/// Lives in the domain layer because <see cref="CustomerFactory"/> — a domain type — needs both
/// lookups: <see cref="GetByExternalIdAsync"/> to decide between registering and reconciling, and
/// <see cref="GetByEmailAsync"/> to enforce email uniqueness.
/// </remarks>
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    /// <summary>
    /// Retrieves the customer provisioned for an identity-provider account.
    /// </summary>
    /// <param name="externalUserId">The identity provider's subject identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching customer, or <see langword="null"/> if none has been provisioned.</returns>
    ValueTask<Customer?> GetByExternalIdAsync(ExternalUserId externalUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the customer holding the specified email address.
    /// </summary>
    /// <param name="email">The email address to look up.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching customer, or <see langword="null"/> if no customer holds that address.</returns>
    ValueTask<Customer?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
}
