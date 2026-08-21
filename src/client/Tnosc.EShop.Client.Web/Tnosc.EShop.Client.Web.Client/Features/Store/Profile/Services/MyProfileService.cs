// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Profile.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Profile.Services;

/// <inheritdoc cref="IMyProfileService" />
internal sealed class MyProfileService(IIdentityApi identityApi) : IMyProfileService
{
    public async Task<ClientResult<MyProfileViewModel>> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        ClientResult<Customer> result = await identityApi.GetMeAsync(cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<MyProfileViewModel>.Failure(problem: result.Problem!);
        }

        return ClientResult<MyProfileViewModel>.Success(value: ToViewModel(customer: result.Value));
    }

    public Task<ClientResult> SaveProfileAsync(MyProfileFormViewModel viewModel, CancellationToken cancellationToken)
    {
        ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);

        if (validation is not null)
        {
            return Task.FromResult(ClientResult.Failure(problem: validation));
        }

        return identityApi.UpdateMyProfileAsync(
            request: new UpdateCustomerProfileRequest(
                FirstName: viewModel.FirstName,
                LastName: viewModel.LastName,
                PhoneNumber: viewModel.PhoneNumber),
            cancellationToken: cancellationToken);
    }

    public Task<ClientResult<Guid>> AddAddressAsync(MyAddressFormViewModel viewModel, CancellationToken cancellationToken)
    {
        ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);

        if (validation is not null)
        {
            return Task.FromResult(ClientResult<Guid>.Failure(problem: validation));
        }

        return identityApi.AddMyAddressAsync(
            request: new AddCustomerAddressRequest(
                Street: viewModel.Street,
                City: viewModel.City,
                PostalCode: viewModel.PostalCode,
                Country: viewModel.Country),
            cancellationToken: cancellationToken);
    }

    public Task<ClientResult> SetDefaultAddressAsync(Guid addressId, CancellationToken cancellationToken) =>
        identityApi.SetMyDefaultAddressAsync(addressId: addressId, cancellationToken: cancellationToken);

    public Task<ClientResult> RemoveAddressAsync(Guid addressId, CancellationToken cancellationToken) =>
        identityApi.RemoveMyAddressAsync(addressId: addressId, cancellationToken: cancellationToken);

    private static MyProfileViewModel ToViewModel(Customer customer) =>
        new()
        {
            Id = customer.Id,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            PhoneNumber = customer.PhoneNumber,
            DefaultAddressId = customer.DefaultAddressId,
            Addresses = [.. customer.Addresses.Select(ToViewModel)],
        };

    private static MyAddressListItemViewModel ToViewModel(CustomerAddress address) =>
        new()
        {
            Id = address.Id,
            Street = address.Street,
            City = address.City,
            PostalCode = address.PostalCode,
            Country = address.Country,
        };
}
