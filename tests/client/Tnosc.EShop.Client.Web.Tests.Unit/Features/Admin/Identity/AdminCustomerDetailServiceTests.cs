// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Admin.Identity;

public sealed class AdminCustomerDetailServiceTests
{
    private readonly IIdentityApi _identityApi = Substitute.For<IIdentityApi>();
    private readonly AdminCustomerDetailService _sut;

    public AdminCustomerDetailServiceTests() => _sut = new AdminCustomerDetailService(identityApi: _identityApi);

    [Fact]
    public async Task GetCustomerByIdAsync_Should_MapTheCustomerAndItsAddressesIntoAViewModel()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var address = new CustomerAddress(Id: Guid.CreateVersion7(), Street: "St", City: "City", PostalCode: "0000", Country: "US");
        var customer = new Customer(
            Id: id,
            Email: "a@b.com",
            FirstName: "Jane",
            LastName: "Doe",
            PhoneNumber: "12345",
            IsActive: true,
            DefaultAddressId: address.Id,
            Addresses: [address]);

        _identityApi.GetCustomerByIdAsync(id: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Customer>.Success(value: customer)));

        // Act
        ClientResult<CustomerDetailViewModel> result = await _sut.GetCustomerByIdAsync(id: id, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(expected: customer.Id);
        result.Value.Email.ShouldBe(expected: customer.Email);
        result.Value.FirstName.ShouldBe(expected: customer.FirstName);
        result.Value.LastName.ShouldBe(expected: customer.LastName);
        result.Value.PhoneNumber.ShouldBe(expected: customer.PhoneNumber);
        result.Value.IsActive.ShouldBeTrue();
        result.Value.DefaultAddressId.ShouldBe(expected: address.Id);
        CustomerAddressListItemViewModel mappedAddress = result.Value.Addresses.ShouldHaveSingleItem();
        mappedAddress.Id.ShouldBe(expected: address.Id);
        mappedAddress.Street.ShouldBe(expected: address.Street);
        mappedAddress.City.ShouldBe(expected: address.City);
        mappedAddress.PostalCode.ShouldBe(expected: address.PostalCode);
        mappedAddress.Country.ShouldBe(expected: address.Country);
    }

    [Fact]
    public async Task SaveProfileAsync_Should_FailWithoutCallingTheApi_When_ARequiredFieldIsMissing()
    {
        // Arrange
        CustomerProfileViewModel viewModel = new() { FirstName = string.Empty, LastName = "Doe" };

        // Act
        ClientResult result = await _sut.SaveProfileAsync(id: Guid.CreateVersion7(), viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _identityApi.DidNotReceive().UpdateCustomerProfileAsync(
            id: Arg.Any<Guid>(),
            request: Arg.Any<UpdateCustomerProfileRequest>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveProfileAsync_Should_MapTheViewModelAndCallTheApi_When_TheViewModelIsValid()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        CustomerProfileViewModel viewModel = new() { FirstName = "Jane", LastName = "Doe", PhoneNumber = "12345" };

        _identityApi.UpdateCustomerProfileAsync(
                id: Arg.Any<Guid>(),
                request: Arg.Any<UpdateCustomerProfileRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.SaveProfileAsync(id: id, viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        await _identityApi.Received(requiredNumberOfCalls: 1).UpdateCustomerProfileAsync(
            id: id,
            request: Arg.Is<UpdateCustomerProfileRequest>(predicate: r =>
                r.FirstName == "Jane" && r.LastName == "Doe" && r.PhoneNumber == "12345"),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAddressAsync_Should_FailWithoutCallingTheApi_When_ARequiredFieldIsMissing()
    {
        // Arrange
        CustomerAddressViewModel viewModel = new() { Street = string.Empty, City = "City", PostalCode = "0000", Country = "US" };

        // Act
        ClientResult<Guid> result = await _sut.AddAddressAsync(id: Guid.CreateVersion7(), viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _identityApi.DidNotReceive().AddCustomerAddressAsync(
            id: Arg.Any<Guid>(),
            request: Arg.Any<AddCustomerAddressRequest>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateCustomerAsync_Should_CallTheApi_WithTheGivenId()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        _identityApi.DeactivateCustomerAsync(id: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.DeactivateCustomerAsync(id: id, cancellationToken: CancellationToken.None);

        // Assert
        await _identityApi.Received(requiredNumberOfCalls: 1).DeactivateCustomerAsync(id: id, cancellationToken: Arg.Any<CancellationToken>());
    }
}
