// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminAddCustomerAddress;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminRemoveCustomerAddress;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminSetDefaultCustomerAddress;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerAddress;
using Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerProfile;
using Tnosc.EShop.Server.Application.Identity.Commands.DeactivateCustomer;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// The admin handlers resolve the target customer by the identifier the route carries, never the
/// caller's own — the opposite lookup from the <c>me</c> family covered by
/// <see cref="CustomerAddressCommandHandlerTests"/>.
/// </summary>
public sealed class AdminCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task AdminUpdateCustomerProfile_Should_Commit_When_TheChangeIsAccepted()
    {
        // Arrange
        Customer customer = await GivenTheCustomerExistsAsync();
        var handler = new AdminUpdateCustomerProfileCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new AdminUpdateCustomerProfileCommand(
            CustomerId: customer.Id.Value,
            FirstName: "Amel",
            LastName: "Operator",
            PhoneNumber: "+21612345678"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.Name.FirstName.ShouldBe(expected: "Amel");
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminUpdateCustomerProfile_Should_ReturnNotFound_When_NoSuchCustomer()
    {
        // Arrange
        GivenNoCustomerExists();
        var handler = new AdminUpdateCustomerProfileCommandHandler(repository: _repository, unitOfWork: _unitOfWork);
        var customerId = Guid.NewGuid();

        // Act
        Result result = await handler.HandleAsync(command: new AdminUpdateCustomerProfileCommand(
            CustomerId: customerId,
            FirstName: "Amel",
            LastName: "Operator",
            PhoneNumber: null));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Customer.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminAddCustomerAddress_Should_ReturnTheNewAddressId_And_Commit()
    {
        // Arrange
        Customer customer = await GivenTheCustomerExistsAsync();
        var handler = new AdminAddCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result<Guid> result = await handler.HandleAsync(command: new AdminAddCustomerAddressCommand(
            CustomerId: customer.Id.Value,
            Street: "12 Rue Neuve",
            City: "Tunis",
            PostalCode: "1001",
            Country: "TN"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: customer.Addresses.Single().Id.Value);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminUpdateCustomerAddress_Should_ReplaceEveryPart()
    {
        // Arrange
        Customer customer = await GivenTheCustomerExistsAsync();
        Result<AddressId> addressId = customer.AddAddress(street: "a", city: "b", postalCode: "c", country: "TN");
        var handler = new AdminUpdateCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new AdminUpdateCustomerAddressCommand(
            CustomerId: customer.Id.Value,
            AddressId: addressId.Value.Value,
            Street: "12 Rue Neuve",
            City: "Tunis",
            PostalCode: "1001",
            Country: "TN"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.Addresses.Single().Street.ShouldBe(expected: "12 Rue Neuve");
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminRemoveCustomerAddress_Should_PropagateTheDefaultAddressConflict_Unchanged()
    {
        // Arrange
        Customer customer = await GivenTheCustomerExistsAsync();
        Result<AddressId> theDefault = customer.AddAddress(street: "a", city: "b", postalCode: "c", country: "TN");
        var handler = new AdminRemoveCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new AdminRemoveCustomerAddressCommand(
            CustomerId: customer.Id.Value,
            AddressId: theDefault.Value.Value));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Address.CannotRemoveDefault");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminSetDefaultCustomerAddress_Should_PropagateNotFound_When_TheCustomerDoesNotHoldIt()
    {
        // Arrange
        Customer customer = await GivenTheCustomerExistsAsync();
        var handler = new AdminSetDefaultCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new AdminSetDefaultCustomerAddressCommand(
            CustomerId: customer.Id.Value,
            AddressId: Guid.NewGuid()));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Address.NotFound");
    }

    [Fact]
    public async Task DeactivateCustomer_Should_Commit_When_TheCustomerIsActive()
    {
        // Arrange
        Customer customer = await GivenTheCustomerExistsAsync();
        var handler = new DeactivateCustomerCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new DeactivateCustomerCommand(CustomerId: customer.Id.Value));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.IsActive.ShouldBeFalse();
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateCustomer_Should_PropagateTheAlreadyDeactivatedConflict_Unchanged()
    {
        // Arrange
        Customer customer = await GivenTheCustomerExistsAsync();
        customer.Deactivate();
        var handler = new DeactivateCustomerCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new DeactivateCustomerCommand(CustomerId: customer.Id.Value));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Customer.AlreadyDeactivated");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateCustomer_Should_ReturnNotFound_When_NoSuchCustomer()
    {
        // Arrange
        GivenNoCustomerExists();
        var handler = new DeactivateCustomerCommandHandler(repository: _repository, unitOfWork: _unitOfWork);
        var customerId = Guid.NewGuid();

        // Act
        Result result = await handler.HandleAsync(command: new DeactivateCustomerCommand(CustomerId: customerId));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Customer.NotFound");
    }

    private async Task<Customer> GivenTheCustomerExistsAsync()
    {
        Customer customer = await CustomerTestFactory.RegisterAsync();

        _repository
            .GetByIdAsync(id: Arg.Any<CustomerId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: customer));

        return customer;
    }

    private void GivenNoCustomerExists() =>
        _repository
            .GetByIdAsync(id: Arg.Any<CustomerId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));
}
