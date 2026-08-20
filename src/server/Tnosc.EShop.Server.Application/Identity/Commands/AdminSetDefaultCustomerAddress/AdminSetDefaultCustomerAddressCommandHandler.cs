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

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminSetDefaultCustomerAddress;

/// <summary>
/// Resolves the target customer by identifier and delegates the change of default address to the
/// aggregate.
/// </summary>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class AdminSetDefaultCustomerAddressCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdminSetDefaultCustomerAddressCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        AdminSetDefaultCustomerAddressCommand command,
        CancellationToken cancellationToken = default)
    {
        Customer? customer = await repository.GetByIdAsync(
            id: CustomerId.From(value: command.CustomerId),
            cancellationToken: cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(customerId: command.CustomerId);
        }

        Result updated = customer.SetDefaultAddress(addressId: AddressId.From(value: command.AddressId));

        if (updated.IsError)
        {
            return updated.Errors.ToArray();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
