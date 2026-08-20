// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Provisions customer profiles under the invariants a single <see cref="Customer"/> instance cannot
/// see on its own.
/// </summary>
/// <remarks>
/// <para>
/// Two rules live here, and both are business decisions rather than orchestration, so neither may
/// appear as an <c>if</c> in a command handler:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>Provision or reconcile.</strong> A client calls the provisioning endpoint after
/// <em>every</em> login, not only the first. Whether that means "register" or "reconcile" is decided
/// by looking the account up by its external id — here, not at the call site.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Email uniqueness.</strong> "No two customers share an email address" spans every customer,
/// so it needs <see cref="ICustomerRepository"/>. A unique index backs it in the database, but the
/// rule is stated here so callers get a <c>Conflict</c> rather than a constraint violation.
/// </description>
/// </item>
/// </list>
/// </remarks>
public static class CustomerFactory
{
    /// <summary>
    /// Returns the customer for an identity-provider account, registering one on first sight and
    /// reconciling the email of an existing one otherwise.
    /// </summary>
    /// <param name="repository">The customer repository consulted for both lookups.</param>
    /// <param name="externalUserId">The identity provider's subject identifier, from the token.</param>
    /// <param name="email">The email address, from the token.</param>
    /// <param name="name">The customer's name, supplied by the client.</param>
    /// <param name="phoneNumber">The customer's optional contact number.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The customer and whether this call created them, or a
    /// <c>Customer.EmailAlreadyRegistered</c> conflict when the address belongs to a different account.
    /// </returns>
    public static async ValueTask<Result<CustomerProvisioning>> ProvisionAsync(
        ICustomerRepository repository,
        ExternalUserId externalUserId,
        Email email,
        PersonName name,
        PhoneNumber? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        Customer? existing = await repository.GetByExternalIdAsync(
            externalUserId: externalUserId,
            cancellationToken: cancellationToken);

        if (existing is not null)
        {
            Result reconciled = await ReconcileAsync(
                repository: repository,
                customer: existing,
                email: email,
                cancellationToken: cancellationToken);

            if (reconciled.IsError)
            {
                return reconciled.Errors.ToArray();
            }

            return new CustomerProvisioning(Customer: existing, WasCreated: false);
        }

        Customer? emailOwner = await repository.GetByEmailAsync(email: email, cancellationToken: cancellationToken);

        if (emailOwner is not null)
        {
            return CustomerErrors.EmailAlreadyRegistered(email: email.Value);
        }

        var registered = Customer.Register(
            externalUserId: externalUserId,
            email: email,
            name: name,
            phoneNumber: phoneNumber);

        // Adding the new customer belongs here, with the decision that produced it, rather than in the
        // handler. The handler cannot do it without asking "was this created?" — an `if` on a domain
        // fact, which is business branching and which NoBusinessBranchingTests rejects. Reconciling an
        // existing customer needs no equivalent call: it was loaded through this same repository and is
        // already tracked. The handler still owns the commit; only the decision lives here.
        await repository.AddAsync(aggregate: registered, cancellationToken: cancellationToken);

        return new CustomerProvisioning(Customer: registered, WasCreated: true);
    }

    // Uniqueness still bites on the reconcile path: the identity provider may have moved this account
    // onto an address another customer already holds. Checking the owner's identity rather than mere
    // existence is what keeps a customer's own unchanged email from colliding with itself.
    private static async ValueTask<Result> ReconcileAsync(
        ICustomerRepository repository,
        Customer customer,
        Email email,
        CancellationToken cancellationToken)
    {
        Customer? emailOwner = await repository.GetByEmailAsync(email: email, cancellationToken: cancellationToken);

        if (emailOwner is not null && emailOwner.Id != customer.Id)
        {
            return CustomerErrors.EmailAlreadyRegistered(email: email.Value);
        }

        return customer.SyncEmail(email: email);
    }
}
