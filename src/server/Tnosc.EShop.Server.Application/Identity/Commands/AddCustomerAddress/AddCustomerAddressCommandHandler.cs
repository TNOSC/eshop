// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AddCustomerAddress;

/// <summary>
/// Resolves the caller's own customer and delegates the addition to the aggregate, which owns the
/// rule that a customer's first address becomes their default.
/// </summary>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class AddCustomerAddressCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddCustomerAddressCommand, Guid>
{
    /// <inheritdoc />
    public async ValueTask<Result<Guid>> HandleAsync(
        AddCustomerAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<ExternalUserId> externalUserId = ExternalUserId.Create(value: command.ExternalUserId);

        if (externalUserId.IsError)
        {
            return externalUserId.Errors.ToArray();
        }

        Customer? customer = await repository.GetByExternalIdAsync(
            externalUserId: externalUserId.Value,
            cancellationToken: cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotProvisioned(externalUserId: externalUserId.Value.Value);
        }

        Result<AddressId> addressId = customer.AddAddress(
            street: command.Street,
            city: command.City,
            postalCode: command.PostalCode,
            country: command.Country);

        if (addressId.IsError)
        {
            return addressId.Errors.ToArray();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return addressId.Value.Value;
    }
}
