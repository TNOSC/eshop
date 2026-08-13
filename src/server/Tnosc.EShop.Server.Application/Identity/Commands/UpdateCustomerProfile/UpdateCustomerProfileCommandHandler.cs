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

namespace Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerProfile;

/// <summary>
/// Resolves the caller's own customer, delegates the change to the aggregate, and commits.
/// </summary>
/// <remarks>
/// The customer is looked up by the caller's own external id, never by an identifier from the
/// request, so a customer structurally cannot reach another customer's profile. That is why no
/// ownership check appears here — there is nothing to check.
/// </remarks>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class UpdateCustomerProfileCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCustomerProfileCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        UpdateCustomerProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<ExternalUserId> externalUserId = ExternalUserId.Create(value: command.ExternalUserId);

        if (externalUserId.IsError)
        {
            return externalUserId.Errors.ToArray();
        }

        Result<PersonName> name = PersonName.Create(firstName: command.FirstName, lastName: command.LastName);

        if (name.IsError)
        {
            return name.Errors.ToArray();
        }

        PhoneNumber? phoneNumber = null;

        if (command.PhoneNumber is not null)
        {
            Result<PhoneNumber> parsed = PhoneNumber.Create(value: command.PhoneNumber);

            if (parsed.IsError)
            {
                return parsed.Errors.ToArray();
            }

            phoneNumber = parsed.Value;
        }

        Customer? customer = await repository.GetByExternalIdAsync(
            externalUserId: externalUserId.Value,
            cancellationToken: cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotProvisioned(externalUserId: externalUserId.Value.Value);
        }

        Result updated = customer.UpdateProfile(name: name.Value, phoneNumber: phoneNumber);

        if (updated.IsError)
        {
            return updated.Errors.ToArray();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
