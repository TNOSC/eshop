// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Profile.ViewModels;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Profile.Services;

/// <summary>
/// <see cref="Pages.MyProfilePage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IIdentityApi"/> for the storefront
/// self-service profile page.
/// </summary>
public interface IMyProfileService
{
    /// <summary>Reads the caller's own profile.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<MyProfileViewModel>> GetMyProfileAsync(CancellationToken cancellationToken);

    /// <summary>Validates and saves the caller's own profile.</summary>
    /// <param name="viewModel">The profile form's current state.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> SaveProfileAsync(MyProfileFormViewModel viewModel, CancellationToken cancellationToken);

    /// <summary>Validates and adds an address to the caller's own profile.</summary>
    /// <param name="viewModel">The add-address form's current state.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Guid>> AddAddressAsync(MyAddressFormViewModel viewModel, CancellationToken cancellationToken);

    /// <summary>Sets the caller's own default address.</summary>
    /// <param name="addressId">The address id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> SetDefaultAddressAsync(Guid addressId, CancellationToken cancellationToken);

    /// <summary>Removes one of the caller's own addresses.</summary>
    /// <param name="addressId">The address id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> RemoveAddressAsync(Guid addressId, CancellationToken cancellationToken);
}
