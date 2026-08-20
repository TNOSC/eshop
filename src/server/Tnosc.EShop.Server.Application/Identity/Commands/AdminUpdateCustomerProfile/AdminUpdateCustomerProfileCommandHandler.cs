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

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerProfile;

/// <summary>
/// Resolves the target customer by identifier and delegates the change to the aggregate.
/// </summary>
/// <remarks>
/// Unlike <c>UpdateCustomerProfileCommandHandler</c>, the customer is looked up by the identifier the
/// route carries rather than the caller's own external id — this is the admin path, gated on
/// <c>identity:write</c> at the endpoint rather than resolved from <c>IUserContext</c>.
/// </remarks>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class AdminUpdateCustomerProfileCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdminUpdateCustomerProfileCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        AdminUpdateCustomerProfileCommand command,
        CancellationToken cancellationToken = default)
    {
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

        Customer? customer = await repository.GetByIdAsync(
            id: CustomerId.From(value: command.CustomerId),
            cancellationToken: cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(customerId: command.CustomerId);
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
