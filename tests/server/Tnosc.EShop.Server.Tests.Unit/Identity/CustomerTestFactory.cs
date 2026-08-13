// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Tnosc.EShop.Server.Domain.Identity.Customers;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// Builds <see cref="Customer"/> instances for tests through the real domain path, never by reaching
/// into private state.
/// </summary>
/// <remarks>
/// <c>Customer.Register</c> is <see langword="internal"/> behind <see cref="CustomerFactory"/>, so the
/// only way in is the factory — which needs a repository. A stub answering "nothing found" to both
/// lookups gives the factory a clean world to register into.
/// </remarks>
internal static class CustomerTestFactory
{
    /// <summary>
    /// Registers a customer through <see cref="CustomerFactory.ProvisionAsync"/> against an empty
    /// repository.
    /// </summary>
    /// <param name="externalUserId">The identity provider's subject identifier.</param>
    /// <param name="email">The customer's email address.</param>
    /// <param name="firstName">The customer's given name.</param>
    /// <param name="lastName">The customer's family name.</param>
    /// <param name="phoneNumber">The customer's optional contact number.</param>
    /// <returns>The registered customer.</returns>
    public static async ValueTask<Customer> RegisterAsync(
        string externalUserId = "keycloak-sub",
        string email = "sami@example.com",
        string firstName = "Sami",
        string lastName = "Shopper",
        string? phoneNumber = null)
    {
        ICustomerRepository repository = EmptyRepository();

        return (await CustomerFactory.ProvisionAsync(
            repository: repository,
            externalUserId: ExternalUserId.Create(value: externalUserId).Value,
            email: Email.Create(value: email).Value,
            name: PersonName.Create(firstName: firstName, lastName: lastName).Value,
            phoneNumber: phoneNumber is null ? null : PhoneNumber.Create(value: phoneNumber).Value)).Value.Customer;
    }

    /// <summary>
    /// A repository stub that finds nothing by either lookup.
    /// </summary>
    /// <returns>The configured substitute.</returns>
    public static ICustomerRepository EmptyRepository()
    {
        ICustomerRepository repository = Substitute.For<ICustomerRepository>();

        repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));

        repository
            .GetByEmailAsync(email: Arg.Any<Email>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));

        return repository;
    }
}
