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

namespace Tnosc.EShop.Server.Application.Identity.Commands.SetDefaultCustomerAddress;

/// <summary>
/// Resolves the caller's own customer and delegates the change of default address to the aggregate.
/// </summary>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class SetDefaultCustomerAddressCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetDefaultCustomerAddressCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        SetDefaultCustomerAddressCommand command,
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

        Result updated = customer.SetDefaultAddress(addressId: AddressId.From(value: command.AddressId));

        if (updated.IsError)
        {
            return updated.Errors.ToArray();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
