// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Identity.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Queries;

/// <summary>
/// Implements <see cref="ICustomerProfileReader"/> by reading Identity's customer read model.
/// </summary>
/// <remarks>
/// <para>
/// The adapter is where the two contexts meet, and the only place they do. Ordering's workflow sees
/// <see cref="CustomerProfileSnapshot"/> — four strings it owns — and never a <c>Customer</c>,
/// a <c>CustomerId</c> or an <c>Address</c>. Exactly the shape of Basket's <c>ProductLookup</c> onto
/// Catalog.
/// </para>
/// <para>
/// Not a query handler: it implements no <c>IQueryHandler&lt;,&gt;</c>, so
/// <c>InfrastructurePersistenceExtensions.AddQueries</c>' scan misses it and it is registered
/// explicitly alongside.
/// </para>
/// <para>
/// The lookup is by <c>external_user_id</c>, not by Identity's own <c>CustomerId</c>. Baskets and
/// orders are both keyed on the caller's identity-provider subject, which is what the token carries
/// and therefore the only identifier the endpoint can supply without a round trip; Identity's surrogate
/// key never leaves Identity.
/// </para>
/// </remarks>
/// <param name="context">The read context.</param>
internal sealed class CustomerProfileReader(EShopReadDbContext context) : ICustomerProfileReader
{
    /// <inheritdoc />
    public async ValueTask<CustomerProfileSnapshot?> GetDefaultAddressAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        string externalUserId = customerId.ToString();

        return await context.Set<CustomerReadModel>()
            .Where(predicate: customer => customer.ExternalUserId == externalUserId && customer.DefaultAddressId != null)
            .SelectMany(selector: customer => customer.Addresses
                .Where(address => address.Id == customer.DefaultAddressId)
                .Select(address => new CustomerProfileSnapshot(
                    Street: address.Street,
                    City: address.City,
                    PostalCode: address.PostalCode,
                    Country: address.Country)))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}
