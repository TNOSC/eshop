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

namespace Tnosc.EShop.Server.Application.Identity.Commands.ProvisionCustomer;

/// <summary>
/// Builds the command's value objects, delegates to <see cref="CustomerFactory"/> — which owns both
/// the provision-or-reconcile decision and the email-uniqueness rule — and commits.
/// </summary>
/// <remarks>
/// <para>
/// No <c>[Transactional]</c>: single aggregate, single commit. No <c>[Idempotent]</c> either, and
/// that is a decision rather than an omission — provisioning is already exactly-once through the
/// external-id lookup, so requiring an <c>Idempotency-Key</c> header on a call the browser makes
/// after every login would be friction with no guarantee gained.
/// </para>
/// <para>
/// Notice there is no branch on whether the customer already existed: the factory adds a newly
/// registered customer to the repository itself, so this handler commits the same way either way and
/// simply carries the domain's <c>WasCreated</c> verdict outwards.
/// </para>
/// </remarks>
/// <param name="repository">The customer repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class ProvisionCustomerCommandHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ProvisionCustomerCommand, ProvisionCustomerResult>
{
    /// <inheritdoc />
    public async ValueTask<Result<ProvisionCustomerResult>> HandleAsync(
        ProvisionCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<ExternalUserId> externalUserId = ExternalUserId.Create(value: command.ExternalUserId);

        if (externalUserId.IsError)
        {
            return externalUserId.Errors.ToArray();
        }

        Result<Email> email = Email.Create(value: command.Email);

        if (email.IsError)
        {
            return email.Errors.ToArray();
        }

        Result<PersonName> name = PersonName.Create(firstName: command.FirstName, lastName: command.LastName);

        if (name.IsError)
        {
            return name.Errors.ToArray();
        }

        // A phone number is optional, so null means "none" and skips parsing entirely. The null test
        // is orchestration plumbing, not a business decision — an absent value has no rule to apply.
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

        Result<CustomerProvisioning> provisioning = await CustomerFactory.ProvisionAsync(
            repository: repository,
            externalUserId: externalUserId.Value,
            email: email.Value,
            name: name.Value,
            phoneNumber: phoneNumber,
            cancellationToken: cancellationToken);

        if (provisioning.IsError)
        {
            return provisioning.Errors.ToArray();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return new ProvisionCustomerResult(
            CustomerId: provisioning.Value.Customer.Id.Value,
            WasCreated: provisioning.Value.WasCreated);
    }
}
