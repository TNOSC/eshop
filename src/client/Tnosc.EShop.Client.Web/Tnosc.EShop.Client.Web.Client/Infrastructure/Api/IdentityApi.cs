// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.EShop.Client.Web.Contracts.Routes;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// <see cref="IIdentityApi"/> against the BFF's Identity endpoints. Contains no absolute URI and
/// never contains the string <c>bff</c> — the difference between the two hosts is entirely in the
/// injected <see cref="HttpClient.BaseAddress"/>.
/// </summary>
internal sealed class IdentityApi(HttpClient httpClient) : IIdentityApi
{
    public async Task<ClientResult<Customer>> GetMeAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Identity.Me,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Customer>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> UpdateMyProfileAsync(UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            requestUri: ApiRoutes.Identity.MeProfile,
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<Guid>> AddMyAddressAsync(AddCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            requestUri: ApiRoutes.Identity.MeAddresses,
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Guid>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> UpdateMyAddressAsync(
        Guid addressId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            requestUri: ApiRoutes.Identity.MeAddressById(addressId: addressId),
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> RemoveMyAddressAsync(Guid addressId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync(
            requestUri: ApiRoutes.Identity.MeAddressById(addressId: addressId),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> SetMyDefaultAddressAsync(Guid addressId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsync(
            requestUri: ApiRoutes.Identity.MeDefaultAddressById(addressId: addressId),
            content: null,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<PagedResult<CustomerSummary>>> SearchCustomersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Identity.SearchCustomers(
                search: search,
                isActive: isActive,
                page: page,
                pageSize: pageSize),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<PagedResult<CustomerSummary>>(
            response: response,
            cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<Customer>> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            requestUri: ApiRoutes.Identity.CustomerById(id: id),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Customer>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> UpdateCustomerProfileAsync(
        Guid id,
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            requestUri: ApiRoutes.Identity.CustomerProfileById(id: id),
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult<Guid>> AddCustomerAddressAsync(
        Guid id,
        AddCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            requestUri: ApiRoutes.Identity.CustomerAddressesById(id: id),
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync<Guid>(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> UpdateCustomerAddressAsync(
        Guid id,
        Guid addressId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            requestUri: ApiRoutes.Identity.CustomerAddressByIdAndAddressId(id: id, addressId: addressId),
            value: request,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> RemoveCustomerAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync(
            requestUri: ApiRoutes.Identity.CustomerAddressByIdAndAddressId(id: id, addressId: addressId),
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> SetDefaultCustomerAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PutAsync(
            requestUri: ApiRoutes.Identity.CustomerDefaultAddressById(id: id, addressId: addressId),
            content: null,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }

    public async Task<ClientResult> DeactivateCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsync(
            requestUri: ApiRoutes.Identity.CustomerDeactivateById(id: id),
            content: null,
            cancellationToken: cancellationToken);

        return await ApiResponseReader.ReadAsync(response: response, cancellationToken: cancellationToken);
    }
}
