// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Identity.ReadModels;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.Queries;

/// <summary>
/// Projects the caller's own customer profile straight from the read context into
/// <see cref="CustomerDto"/>, matched on the identity provider's subject identifier.
/// </summary>
/// <param name="context">The read context.</param>
internal sealed class GetCurrentCustomerQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetCurrentCustomerQuery, CustomerDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<CustomerDto>> HandleAsync(
        GetCurrentCustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        CustomerDto? customer = await context.Set<CustomerReadModel>()
            .Where(predicate: readModel => readModel.ExternalUserId == query.ExternalUserId)
            .Select(selector: readModel => new CustomerDto(
                Id: readModel.Id,
                Email: readModel.Email,
                FirstName: readModel.FirstName,
                LastName: readModel.LastName,
                PhoneNumber: readModel.PhoneNumber,
                IsActive: readModel.IsActive,
                DefaultAddressId: readModel.DefaultAddressId,
                Addresses: readModel.Addresses
                    .Select(address => new CustomerAddressDto(
                        Id: address.Id,
                        Street: address.Street,
                        City: address.City,
                        PostalCode: address.PostalCode,
                        Country: address.Country))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotProvisioned(externalUserId: query.ExternalUserId ?? string.Empty);
        }

        return customer;
    }
}
