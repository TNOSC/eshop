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
using Tnosc.EShop.Client.Web.Client.Features.Store.Profile.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Profile.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Features.Store.Profile;

public sealed class MyProfileServiceTests
{
    private readonly IIdentityApi _identityApi = Substitute.For<IIdentityApi>();
    private readonly MyProfileService _sut;

    public MyProfileServiceTests() => _sut = new MyProfileService(identityApi: _identityApi);

    [Fact]
    public async Task GetMyProfileAsync_Should_MapTheCustomerAndItsAddressesIntoAViewModel()
    {
        // Arrange
        var address = new CustomerAddress(Id: Guid.CreateVersion7(), Street: "St", City: "City", PostalCode: "0000", Country: "US");
        var customer = new Customer(
            Id: Guid.CreateVersion7(),
            Email: "a@b.com",
            FirstName: "Jane",
            LastName: "Doe",
            PhoneNumber: "12345",
            IsActive: true,
            DefaultAddressId: address.Id,
            Addresses: [address]);

        _identityApi.GetMeAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Customer>.Success(value: customer)));

        // Act
        ClientResult<MyProfileViewModel> result = await _sut.GetMyProfileAsync(cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(expected: customer.Id);
        result.Value.Email.ShouldBe(expected: customer.Email);
        result.Value.FirstName.ShouldBe(expected: customer.FirstName);
        result.Value.LastName.ShouldBe(expected: customer.LastName);
        result.Value.PhoneNumber.ShouldBe(expected: customer.PhoneNumber);
        result.Value.DefaultAddressId.ShouldBe(expected: address.Id);
        MyAddressListItemViewModel mappedAddress = result.Value.Addresses.ShouldHaveSingleItem();
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
        MyProfileFormViewModel viewModel = new() { FirstName = string.Empty, LastName = "Doe" };

        // Act
        ClientResult result = await _sut.SaveProfileAsync(viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _identityApi.DidNotReceive().UpdateMyProfileAsync(
            request: Arg.Any<UpdateCustomerProfileRequest>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveProfileAsync_Should_MapTheViewModelAndCallTheApi_When_TheViewModelIsValid()
    {
        // Arrange
        MyProfileFormViewModel viewModel = new() { FirstName = "Jane", LastName = "Doe", PhoneNumber = "12345" };

        _identityApi.UpdateMyProfileAsync(
                request: Arg.Any<UpdateCustomerProfileRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.SaveProfileAsync(viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        await _identityApi.Received(requiredNumberOfCalls: 1).UpdateMyProfileAsync(
            request: Arg.Is<UpdateCustomerProfileRequest>(predicate: r =>
                r.FirstName == "Jane" && r.LastName == "Doe" && r.PhoneNumber == "12345"),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAddressAsync_Should_FailWithoutCallingTheApi_When_ARequiredFieldIsMissing()
    {
        // Arrange
        MyAddressFormViewModel viewModel = new() { Street = string.Empty, City = "City", PostalCode = "0000", Country = "US" };

        // Act
        ClientResult<Guid> result = await _sut.AddAddressAsync(viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _identityApi.DidNotReceive().AddMyAddressAsync(
            request: Arg.Any<AddCustomerAddressRequest>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAddressAsync_Should_MapTheViewModelAndCallTheApi_When_TheViewModelIsValid()
    {
        // Arrange
        MyAddressFormViewModel viewModel = new() { Street = "St", City = "City", PostalCode = "0000", Country = "US" };
        var addressId = Guid.CreateVersion7();

        _identityApi.AddMyAddressAsync(
                request: Arg.Any<AddCustomerAddressRequest>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult<Guid>.Success(value: addressId)));

        // Act
        ClientResult<Guid> result = await _sut.AddAddressAsync(viewModel: viewModel, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: addressId);
        await _identityApi.Received(requiredNumberOfCalls: 1).AddMyAddressAsync(
            request: Arg.Is<AddCustomerAddressRequest>(predicate: r =>
                r.Street == "St" && r.City == "City" && r.PostalCode == "0000" && r.Country == "US"),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefaultAddressAsync_Should_CallTheApi_WithTheGivenAddressId()
    {
        // Arrange
        var addressId = Guid.CreateVersion7();
        _identityApi.SetMyDefaultAddressAsync(addressId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.SetDefaultAddressAsync(addressId: addressId, cancellationToken: CancellationToken.None);

        // Assert
        await _identityApi.Received(requiredNumberOfCalls: 1).SetMyDefaultAddressAsync(addressId: addressId, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAddressAsync_Should_CallTheApi_WithTheGivenAddressId()
    {
        // Arrange
        var addressId = Guid.CreateVersion7();
        _identityApi.RemoveMyAddressAsync(addressId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Task.FromResult(ClientResult.Success()));

        // Act
        await _sut.RemoveAddressAsync(addressId: addressId, cancellationToken: CancellationToken.None);

        // Assert
        await _identityApi.Received(requiredNumberOfCalls: 1).RemoveMyAddressAsync(addressId: addressId, cancellationToken: Arg.Any<CancellationToken>());
    }
}
