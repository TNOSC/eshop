// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerAddress;

/// <summary>
/// Resolves the target customer by identifier and delegates the change to the aggregate, which owns
/// both finding the address and validating its new contents.
/// </summary>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class AdminUpdateCustomerAddressCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdminUpdateCustomerAddressCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        AdminUpdateCustomerAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        Customer? customer = await repository.GetByIdAsync(
            id: CustomerId.From(value: command.CustomerId),
            cancellationToken: cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(customerId: command.CustomerId);
        }

        Result updated = customer.UpdateAddress(
            addressId: AddressId.From(value: command.AddressId),
            street: command.Street,
            city: command.City,
            postalCode: command.PostalCode,
            country: command.Country);

        if (updated.IsError)
        {
            return updated.Errors.ToArray();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
