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
using Tnosc.EShop.Server.Application.Identity.Commands.AddCustomerAddress;
using Tnosc.EShop.Server.Application.Identity.Commands.RemoveCustomerAddress;
using Tnosc.EShop.Server.Application.Identity.Commands.SetDefaultCustomerAddress;
using Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerProfile;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Identity;

/// <summary>
/// The <c>me</c> handlers resolve the caller's own customer by external id and propagate the
/// aggregate's verdict. None of them contains an ownership check, because none of them can address
/// another customer in the first place.
/// </summary>
public sealed class CustomerAddressCommandHandlerTests
{
    private const string CallerSubject = "sub-1";

    private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task AddAddress_Should_ReturnTheNewAddressId_And_Commit()
    {
        // Arrange
        Customer customer = await GivenTheCallerHasAProfileAsync();
        var handler = new AddCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result<Guid> result = await handler.HandleAsync(command: new AddCustomerAddressCommand(
            ExternalUserId: CallerSubject,
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
    public async Task AddAddress_Should_PropagateTheValidationError_And_NotCommit()
    {
        // Arrange
        await GivenTheCallerHasAProfileAsync();
        var handler = new AddCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result<Guid> result = await handler.HandleAsync(command: new AddCustomerAddressCommand(
            ExternalUserId: CallerSubject,
            Street: null,
            City: "Tunis",
            PostalCode: "1001",
            Country: "TN"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Address.StreetRequired");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAddress_Should_ReturnNotProvisioned_When_TheCallerHasNoProfileYet()
    {
        // Arrange
        _repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: null));
        var handler = new AddCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result<Guid> result = await handler.HandleAsync(command: new AddCustomerAddressCommand(
            ExternalUserId: CallerSubject,
            Street: "12 Rue Neuve",
            City: "Tunis",
            PostalCode: "1001",
            Country: "TN"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Customer.NotProvisioned");
    }

    [Fact]
    public async Task RemoveAddress_Should_PropagateTheDefaultAddressConflict_Unchanged()
    {
        // Arrange
        Customer customer = await GivenTheCallerHasAProfileAsync();
        Result<AddressId> theDefault = customer.AddAddress(street: "a", city: "b", postalCode: "c", country: "TN");
        var handler = new RemoveCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new RemoveCustomerAddressCommand(
            ExternalUserId: CallerSubject,
            AddressId: theDefault.Value.Value));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Address.CannotRemoveDefault");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefaultAddress_Should_PropagateNotFound_When_TheCallerDoesNotHoldIt()
    {
        // Arrange
        await GivenTheCallerHasAProfileAsync();
        var handler = new SetDefaultCustomerAddressCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new SetDefaultCustomerAddressCommand(
            ExternalUserId: CallerSubject,
            AddressId: Guid.NewGuid()));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Address.NotFound");
    }

    [Fact]
    public async Task UpdateProfile_Should_PropagateTheDeactivatedConflict_Unchanged()
    {
        // Arrange
        Customer customer = await GivenTheCallerHasAProfileAsync();
        customer.Deactivate();
        var handler = new UpdateCustomerProfileCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new UpdateCustomerProfileCommand(
            ExternalUserId: CallerSubject,
            FirstName: "Amel",
            LastName: "Operator",
            PhoneNumber: null));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Customer.Deactivated");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_Should_Commit_When_TheChangeIsAccepted()
    {
        // Arrange
        Customer customer = await GivenTheCallerHasAProfileAsync();
        var handler = new UpdateCustomerProfileCommandHandler(repository: _repository, unitOfWork: _unitOfWork);

        // Act
        Result result = await handler.HandleAsync(command: new UpdateCustomerProfileCommand(
            ExternalUserId: CallerSubject,
            FirstName: "Amel",
            LastName: "Operator",
            PhoneNumber: "+21612345678"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        customer.Name.FirstName.ShouldBe(expected: "Amel");
        customer.PhoneNumber!.Value.ShouldBe(expected: "+21612345678");
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private async Task<Customer> GivenTheCallerHasAProfileAsync()
    {
        Customer customer = await CustomerTestFactory.RegisterAsync(externalUserId: CallerSubject);

        _repository
            .GetByExternalIdAsync(externalUserId: Arg.Any<ExternalUserId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Customer?>(result: customer));

        return customer;
    }
}
