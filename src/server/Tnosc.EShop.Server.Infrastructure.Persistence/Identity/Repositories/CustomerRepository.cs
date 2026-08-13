// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Infrastructure.Persistence;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.Repositories;

/// <summary>
/// The write-side <see cref="Customer"/> repository.
/// </summary>
/// <remarks>
/// Both lookups exist for <see cref="CustomerFactory"/>, which needs them to decide between
/// registering and reconciling and to enforce email uniqueness. Addresses come back with the customer
/// without an explicit <c>Include</c> — the configuration marks the owned collection
/// <c>AutoInclude</c>, so every aggregate the domain reasons over is whole.
/// </remarks>
/// <param name="context">The write context this repository reads and writes through.</param>
internal sealed class CustomerRepository(EShopWriteDbContext context)
    : RepositoryBase<Customer, CustomerId>(context), ICustomerRepository
{
    /// <inheritdoc />
    public async ValueTask<Customer?> GetByExternalIdAsync(
        ExternalUserId externalUserId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Customer>()
            .FirstOrDefaultAsync(
                predicate: customer => customer.ExternalUserId.Value == externalUserId.Value,
                cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async ValueTask<Customer?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default) =>
        await context.Set<Customer>()
            .FirstOrDefaultAsync(
                predicate: customer => customer.Email.Value == email.Value,
                cancellationToken: cancellationToken);
}
