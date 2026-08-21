// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Profile.ViewModels;

/// <summary>
/// A single address on the caller's own profile, as listed (read-only) by <c>MyProfilePage</c>'s
/// addresses grid, nested under <see cref="MyProfileViewModel"/>. Disambiguated from the editable
/// <see cref="MyAddressFormViewModel"/> (the add-address form's ViewModel) — read-only display data
/// mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Identity.CustomerAddress"/> by
/// <c>MyProfileService</c>, so it carries no DataAnnotations.
/// </summary>
public sealed class MyAddressListItemViewModel
{
    /// <summary>Gets or sets the address id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the street line.</summary>
    public string Street { get; init; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    public string City { get; init; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; init; } = string.Empty;

    /// <summary>Gets or sets the country.</summary>
    public string Country { get; init; } = string.Empty;
}
