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
using Tnosc.EShop.Server.Application.Identity.Queries.GetCustomerById;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Identity.ReadModels;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.Queries;

/// <summary>
/// Projects any customer's profile straight from the read context into <see cref="CustomerDto"/>,
/// matched on the customer's own identifier.
/// </summary>
/// <param name="context">The read context.</param>
internal sealed class GetCustomerByIdQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetCustomerByIdQuery, CustomerDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<CustomerDto>> HandleAsync(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        CustomerDto? customer = await context.Set<CustomerReadModel>()
            .Where(predicate: readModel => readModel.Id == query.CustomerId)
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
            return CustomerErrors.NotFound(customerId: query.CustomerId);
        }

        return customer;
    }
}
