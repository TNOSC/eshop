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
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Identity.Commands.RemoveCustomerAddress;

/// <summary>
/// Resolves the caller's own customer and delegates the removal to the aggregate, which owns the rule
/// that the default address cannot be removed.
/// </summary>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class RemoveCustomerAddressCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveCustomerAddressCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        RemoveCustomerAddressCommand command,
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

        Result removed = customer.RemoveAddress(addressId: AddressId.From(value: command.AddressId));

        if (removed.IsError)
        {
            return removed.Errors.ToArray();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
