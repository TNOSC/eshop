// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Domain.Identity.Customers.Events;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// The transitions <see cref="Customer"/> owns: registration, email reconciliation, profile edits and
/// the address-collection invariants.
/// </summary>
public sealed class CustomerTests
{
    private readonly Faker _faker = IdentityFaker.New();

    [Fact]
    public async Task Register_Should_RaiseCustomerRegisteredDomainEvent_WithAFullPayload()
    {
        // Arrange & Act
        Customer customer = await CustomerTestFactory.RegisterAsync(
            externalUserId: "sub-1",
            email: "sami@example.com",
            firstName: "Sami",
            lastName: "Shopper");

        // Assert
        CustomerRegisteredDomainEvent raised = customer.DomainEvents.OfType<CustomerRegisteredDomainEvent>().ShouldHaveSingleItem();
        raised.CustomerId.ShouldBe(expected: customer.Id.Value);
        raised.ExternalUserId.ShouldBe(expected: "sub-1");
        raised.Email.ShouldBe(expected: "sami@example.com");
        raised.FirstName.ShouldBe(expected: "Sami");
        raised.LastName.ShouldBe(expected: "Shopper");
        raised.Id.ShouldNotBe(expected: Guid.Empty);
        raised.OccurredOnUtc.ShouldNotBe(expected: DateTime.MinValue);
    }

    [Fact]
    public async Task Register_Should_ActivateTheCustomer_And_IncrementTheVersion()
    {
        // Act
        Customer customer = await CustomerTestFactory.RegisterAsync();

        // Assert
        customer.IsActive.ShouldBeTrue();
        customer.Version.ShouldBe(expected: 1);
        customer.Addresses.ShouldBeEmpty();
        customer.DefaultAddressId.ShouldBeNull();
    }

    // The reason SyncEmail exists at all: the client calls the provisioning path after every login, so
    // an unconditional assignment would raise an event — and write an outbox row — once per request.
    [Fact]
    public async Task SyncEmail_Should_RaiseNoEvent_And_NotBumpTheVersion_When_TheEmailIsUnchanged()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync(email: "sami@example.com");
        customer.ClearDomainEvents();
        int versionBefore = customer.Version;

        // Act
        Result result = await ReconcileAsync(customer: customer, email: "sami@example.com");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.DomainEvents.ShouldBeEmpty(customMessage: "A repeat login must not produce an outbox row.");
        customer.Version.ShouldBe(expected: versionBefore);
    }

    [Fact]
    public async Task SyncEmail_Should_RaiseNoEvent_When_TheEmailDiffersOnlyByCase()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync(email: "sami@example.com");
        customer.ClearDomainEvents();

        // Act
        await ReconcileAsync(customer: customer, email: "SAMI@EXAMPLE.COM");

        // Assert
        customer.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task SyncEmail_Should_RaiseCustomerEmailChangedDomainEvent_When_TheEmailActuallyChanged()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync(email: "old@example.com");
        customer.ClearDomainEvents();

        // Act
        Result result = await ReconcileAsync(customer: customer, email: "new@example.com");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.Email.Value.ShouldBe(expected: "new@example.com");

        CustomerEmailChangedDomainEvent raised = customer.DomainEvents.OfType<CustomerEmailChangedDomainEvent>().ShouldHaveSingleItem();
        raised.OldEmail.ShouldBe(expected: "old@example.com");
        raised.NewEmail.ShouldBe(expected: "new@example.com");
    }

    [Fact]
    public async Task AddAddress_Should_MakeTheFirstAddressTheDefault()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();

        // Act
        Result<AddressId> first = AddAddress(customer: customer);
        Result<AddressId> second = AddAddress(customer: customer);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        customer.Addresses.Count.ShouldBe(expected: 2);
        customer.DefaultAddressId.ShouldBe(expected: first.Value, customMessage: "The first address added becomes the default, and a later one must not steal it.");
    }

    [Fact]
    public async Task AddAddress_Should_PropagateTheValidationError_When_TheCountryIsNotIso2()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();

        // Act
        Result<AddressId> result = customer.AddAddress(
            street: _faker.Street(),
            city: _faker.City(),
            postalCode: _faker.PostalCode(),
            country: "TUN");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Address.InvalidCountry");
        customer.Addresses.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveAddress_Should_ReturnConflict_When_TheAddressIsTheDefault()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();
        Result<AddressId> theDefault = AddAddress(customer: customer);

        // Act
        Result result = customer.RemoveAddress(addressId: theDefault.Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Address.CannotRemoveDefault");
        customer.Addresses.Count.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task RemoveAddress_Should_Succeed_When_TheAddressIsNotTheDefault()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();
        AddAddress(customer: customer);
        Result<AddressId> spare = AddAddress(customer: customer);

        // Act
        Result result = customer.RemoveAddress(addressId: spare.Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.Addresses.Count.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task RemoveAddress_Should_Succeed_AfterAnotherAddressIsMadeTheDefault()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();
        Result<AddressId> original = AddAddress(customer: customer);
        Result<AddressId> replacement = AddAddress(customer: customer);

        // Act
        Result promoted = customer.SetDefaultAddress(addressId: replacement.Value);
        Result removed = customer.RemoveAddress(addressId: original.Value);

        // Assert
        promoted.IsSuccess.ShouldBeTrue();
        removed.IsSuccess.ShouldBeTrue();
        customer.DefaultAddressId.ShouldBe(expected: replacement.Value);
        customer.Addresses.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RemoveAddress_Should_ReturnNotFound_When_TheCustomerDoesNotHoldIt()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();

        // Act
        Result result = customer.RemoveAddress(addressId: AddressId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Address.NotFound");
    }

    [Fact]
    public async Task UpdateAddress_Should_ReplaceEveryPart()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();
        Result<AddressId> addressId = AddAddress(customer: customer);

        // Act
        Result result = customer.UpdateAddress(
            addressId: addressId.Value,
            street: "12 Rue Neuve",
            city: "Tunis",
            postalCode: "1001",
            country: "tn");

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Address updated = customer.Addresses.ShouldHaveSingleItem();
        updated.Street.ShouldBe(expected: "12 Rue Neuve");
        updated.City.ShouldBe(expected: "Tunis");
        updated.PostalCode.ShouldBe(expected: "1001");
        updated.Country.ShouldBe(expected: "TN", customMessage: "The country code is normalised to uppercase.");
    }

    [Fact]
    public async Task SetDefaultAddress_Should_ReturnNotFound_When_TheCustomerDoesNotHoldIt()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();

        // Act
        Result result = customer.SetDefaultAddress(addressId: AddressId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Address.NotFound");
    }

    [Fact]
    public async Task UpdateProfile_Should_ReplaceTheNameAndPhoneNumber()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();

        // Act
        Result result = customer.UpdateProfile(
            name: PersonName.Create(firstName: "Amel", lastName: "Operator").Value,
            phoneNumber: PhoneNumber.Create(value: "+21612345678").Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.Name.FirstName.ShouldBe(expected: "Amel");
        customer.PhoneNumber!.Value.ShouldBe(expected: "+21612345678");
    }

    [Fact]
    public async Task UpdateProfile_Should_ClearThePhoneNumber_When_NullIsSupplied()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync(phoneNumber: "+21612345678");

        // Act
        customer.UpdateProfile(name: customer.Name, phoneNumber: null);

        // Assert
        customer.PhoneNumber.ShouldBeNull();
    }

    [Fact]
    public async Task Deactivate_Should_BlockEverySubsequentChange()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();
        Result<AddressId> addressId = AddAddress(customer: customer);

        // Act
        Result deactivated = customer.Deactivate();

        // Assert
        deactivated.IsSuccess.ShouldBeTrue();
        customer.IsActive.ShouldBeFalse();

        customer.UpdateProfile(name: customer.Name, phoneNumber: null).FirstError.Code.ShouldBe(expected: "Customer.Deactivated");
        customer.AddAddress(street: "a", city: "b", postalCode: "c", country: "TN").FirstError.Code.ShouldBe(expected: "Customer.Deactivated");
        customer.UpdateAddress(addressId: addressId.Value, street: "a", city: "b", postalCode: "c", country: "TN").FirstError.Code.ShouldBe(expected: "Customer.Deactivated");
        customer.RemoveAddress(addressId: addressId.Value).FirstError.Code.ShouldBe(expected: "Customer.Deactivated");
        customer.SetDefaultAddress(addressId: addressId.Value).FirstError.Code.ShouldBe(expected: "Customer.Deactivated");
    }

    [Fact]
    public async Task Deactivate_Should_RaiseCustomerDeactivatedDomainEvent()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();
        customer.ClearDomainEvents();

        // Act
        Result result = customer.Deactivate();

        // Assert
        result.IsSuccess.ShouldBeTrue();

        CustomerDeactivatedDomainEvent raised = customer.DomainEvents.OfType<CustomerDeactivatedDomainEvent>().ShouldHaveSingleItem();
        raised.CustomerId.ShouldBe(expected: customer.Id.Value);
        raised.Id.ShouldNotBe(expected: Guid.Empty);
        raised.OccurredOnUtc.ShouldNotBe(expected: DateTime.MinValue);
    }

    [Fact]
    public async Task Deactivate_Should_ReturnConflict_When_TheCustomerIsAlreadyDeactivated()
    {
        // Arrange
        Customer customer = await CustomerTestFactory.RegisterAsync();
        customer.Deactivate();

        // Act
        Result result = customer.Deactivate();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Customer.AlreadyDeactivated");
    }

    private Result<AddressId> AddAddress(Customer customer) =>
        customer.AddAddress(
            street: _faker.Street(),
            city: _faker.City(),
            postalCode: _faker.PostalCode(),
            country: _faker.Country());

    // SyncEmail is internal to the domain, reached only through the factory's reconcile path — which
    // is exactly how production reaches it too.
    private static async ValueTask<Result> ReconcileAsync(Customer customer, string email)
    {
        ICustomerRepository repository = CustomerTestFactory.EmptyRepository();

        repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: customer));

        return await CustomerFactory.ProvisionAsync(
            repository: repository,
            externalUserId: customer.ExternalUserId,
            email: Email.Create(value: email).Value,
            name: customer.Name,
            phoneNumber: null);
    }
}
