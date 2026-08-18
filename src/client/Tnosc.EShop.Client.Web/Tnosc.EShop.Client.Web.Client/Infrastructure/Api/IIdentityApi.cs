// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>The typed client for the Identity bounded context's API.</summary>
public interface IIdentityApi
{
    /// <summary>Reads the caller's own profile.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Customer>> GetMeAsync(CancellationToken cancellationToken);

    /// <summary>Lists customers, one page at a time. Requires the <c>identity:read</c> permission.</summary>
    /// <param name="search">An optional free-text search term.</param>
    /// <param name="isActive">An optional active-status filter.</param>
    /// <param name="page">The requested page.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<PagedResult<CustomerSummary>>> SearchCustomersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Reads any customer's profile by identifier. Requires the <c>identity:read</c> permission.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Customer>> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Updates a customer's name and phone number. Requires the <c>identity:write</c> permission.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="request">The new name and phone number.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> UpdateCustomerProfileAsync(
        Guid id,
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken);

    /// <summary>Adds an address to a customer's profile. Requires the <c>identity:write</c> permission.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="request">The address to add.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Guid>> AddCustomerAddressAsync(
        Guid id,
        AddCustomerAddressRequest request,
        CancellationToken cancellationToken);

    /// <summary>Replaces one of a customer's addresses. Requires the <c>identity:write</c> permission.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="addressId">The address id.</param>
    /// <param name="request">The new address content.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> UpdateCustomerAddressAsync(
        Guid id,
        Guid addressId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken);

    /// <summary>Removes one of a customer's addresses. Requires the <c>identity:write</c> permission.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="addressId">The address id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> RemoveCustomerAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken);

    /// <summary>Sets a customer's default address. Requires the <c>identity:write</c> permission.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="addressId">The address id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> SetDefaultCustomerAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken);

    /// <summary>Deactivates a customer's profile. Requires the <c>identity:write</c> permission.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> DeactivateCustomerAsync(Guid id, CancellationToken cancellationToken);
}
