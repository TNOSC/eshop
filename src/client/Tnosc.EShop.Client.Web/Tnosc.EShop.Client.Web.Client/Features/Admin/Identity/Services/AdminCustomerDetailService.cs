// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;

/// <inheritdoc cref="IAdminCustomerDetailService" />
internal sealed class AdminCustomerDetailService(IIdentityApi identityApi) : IAdminCustomerDetailService
{
    public Task<ClientResult<Customer>> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken) =>
        identityApi.GetCustomerByIdAsync(id: id, cancellationToken: cancellationToken);

    public Task<ClientResult> SaveProfileAsync(Guid id, CustomerProfileViewModel viewModel, CancellationToken cancellationToken)
    {
        ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);

        if (validation is not null)
        {
            return Task.FromResult(ClientResult.Failure(problem: validation));
        }

        return identityApi.UpdateCustomerProfileAsync(
            id: id,
            request: new UpdateCustomerProfileRequest(
                FirstName: viewModel.FirstName,
                LastName: viewModel.LastName,
                PhoneNumber: viewModel.PhoneNumber),
            cancellationToken: cancellationToken);
    }

    public Task<ClientResult<Guid>> AddAddressAsync(Guid id, CustomerAddressViewModel viewModel, CancellationToken cancellationToken)
    {
        ClientProblem? validation = ClientValidation.Validate(viewModel: viewModel);

        if (validation is not null)
        {
            return Task.FromResult(ClientResult<Guid>.Failure(problem: validation));
        }

        return identityApi.AddCustomerAddressAsync(
            id: id,
            request: new AddCustomerAddressRequest(
                Street: viewModel.Street,
                City: viewModel.City,
                PostalCode: viewModel.PostalCode,
                Country: viewModel.Country),
            cancellationToken: cancellationToken);
    }

    public Task<ClientResult> SetDefaultAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken) =>
        identityApi.SetDefaultCustomerAddressAsync(id: id, addressId: addressId, cancellationToken: cancellationToken);

    public Task<ClientResult> RemoveAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken) =>
        identityApi.RemoveCustomerAddressAsync(id: id, addressId: addressId, cancellationToken: cancellationToken);

    public Task<ClientResult> DeactivateCustomerAsync(Guid id, CancellationToken cancellationToken) =>
        identityApi.DeactivateCustomerAsync(id: id, cancellationToken: cancellationToken);
}
