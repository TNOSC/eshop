// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;

/// <summary>
/// <see cref="Pages.AdminCustomerDetailPage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IIdentityApi"/> for the admin
/// customer-detail page.
/// </summary>
public interface IAdminCustomerDetailService
{
    /// <summary>Reads any customer's profile by identifier.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<CustomerDetailViewModel>> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Validates and saves a customer's profile.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="viewModel">The profile form's current state.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> SaveProfileAsync(Guid id, CustomerProfileViewModel viewModel, CancellationToken cancellationToken);

    /// <summary>Validates and adds an address to a customer's profile.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="viewModel">The add-address form's current state.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Guid>> AddAddressAsync(Guid id, CustomerAddressViewModel viewModel, CancellationToken cancellationToken);

    /// <summary>Sets a customer's default address.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="addressId">The address id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> SetDefaultAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken);

    /// <summary>Removes one of a customer's addresses.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="addressId">The address id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> RemoveAddressAsync(Guid id, Guid addressId, CancellationToken cancellationToken);

    /// <summary>Deactivates a customer's profile.</summary>
    /// <param name="id">The customer id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> DeactivateCustomerAsync(Guid id, CancellationToken cancellationToken);
}
