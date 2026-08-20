// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// Provision-or-reconcile and email uniqueness are business decisions, so they live in the domain
/// where the repository contract is reachable — never as an <c>if</c> in a command handler.
/// </summary>
public sealed class CustomerFactoryTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

    public CustomerFactoryTests()
    {
        _repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));

        _repository
            .GetByEmailAsync(email: Arg.Any<Email>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));
    }

    [Fact]
    public async Task ProvisionAsync_Should_Register_And_ReportWasCreated_When_TheAccountIsNew()
    {
        // Act
        Result<CustomerProvisioning> result = await ProvisionAsync(externalUserId: "sub-1", email: "sami@example.com");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.WasCreated.ShouldBeTrue();
        result.Value.Customer.ExternalUserId.Value.ShouldBe(expected: "sub-1");
        result.Value.Customer.Email.Value.ShouldBe(expected: "sami@example.com");
    }

    // The handler cannot add the new customer itself without branching on WasCreated, so the factory
    // does it — this is the test that keeps that contract honest.
    [Fact]
    public async Task ProvisionAsync_Should_AddTheNewCustomerToTheRepository_When_TheAccountIsNew()
    {
        // Act
        Result<CustomerProvisioning> result = await ProvisionAsync(externalUserId: "sub-1", email: "sami@example.com");

        // Assert
        await _repository.Received(requiredNumberOfCalls: 1).AddAsync(
            aggregate: result.Value.Customer,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_Should_Reconcile_And_ReportNotCreated_When_TheAccountAlreadyHasAProfile()
    {
        // Arrange
        Customer existing = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-1", email: "old@example.com");
        GivenExistingByExternalId(customer: existing);

        // Act
        Result<CustomerProvisioning> result = await ProvisionAsync(externalUserId: "sub-1", email: "new@example.com");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.WasCreated.ShouldBeFalse();
        result.Value.Customer.ShouldBeSameAs(expected: existing);
        result.Value.Customer.Email.Value.ShouldBe(expected: "new@example.com");
    }

    [Fact]
    public async Task ProvisionAsync_Should_NotAddToTheRepository_When_TheAccountAlreadyHasAProfile()
    {
        // Arrange
        Customer existing = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-1");
        GivenExistingByExternalId(customer: existing);

        // Act
        await ProvisionAsync(externalUserId: "sub-1", email: existing.Email.Value);

        // Assert
        await _repository.DidNotReceive().AddAsync(
            aggregate: Arg.Any<Customer>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_Should_ReturnConflict_When_TheEmailBelongsToADifferentAccount()
    {
        // Arrange
        Customer other = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-other", email: "taken@example.com");
        _repository
            .GetByEmailAsync(email: Arg.Any<Email>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: other));

        // Act
        Result<CustomerProvisioning> result = await ProvisionAsync(externalUserId: "sub-new", email: "taken@example.com");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Customer.EmailAlreadyRegistered");
    }

    // A customer's own unchanged address must not collide with itself on the reconcile path: the check
    // is "does someone ELSE hold it", not "does anyone hold it".
    [Fact]
    public async Task ProvisionAsync_Should_Succeed_When_TheEmailIsAlreadyHeldByTheSameAccount()
    {
        // Arrange
        Customer existing = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-1", email: "sami@example.com");
        GivenExistingByExternalId(customer: existing);
        _repository
            .GetByEmailAsync(email: Arg.Any<Email>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: existing));

        // Act
        Result<CustomerProvisioning> result = await ProvisionAsync(externalUserId: "sub-1", email: "sami@example.com");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.WasCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProvisionAsync_Should_ReturnConflict_When_ReconcilingOntoAnotherCustomersEmail()
    {
        // Arrange
        Customer existing = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-1", email: "mine@example.com");
        Customer other = await CustomerTestFactory.RegisterAsync(externalUserId: "sub-2", email: "theirs@example.com");
        GivenExistingByExternalId(customer: existing);
        _repository
            .GetByEmailAsync(email: Arg.Any<Email>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: other));

        // Act
        Result<CustomerProvisioning> result = await ProvisionAsync(externalUserId: "sub-1", email: "theirs@example.com");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Customer.EmailAlreadyRegistered");
        existing.Email.Value.ShouldBe(expected: "mine@example.com", customMessage: "A rejected reconciliation must leave the customer untouched.");
    }

    private void GivenExistingByExternalId(Customer customer) =>
        _repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: customer));

    private ValueTask<Result<CustomerProvisioning>> ProvisionAsync(string externalUserId, string email) =>
        CustomerFactory.ProvisionAsync(
            repository: _repository,
            externalUserId: ExternalUserId.Create(value: externalUserId).Value,
            email: Email.Create(value: email).Value,
            name: PersonName.Create(firstName: "Sami", lastName: "Shopper").Value,
            phoneNumber: null);
}
