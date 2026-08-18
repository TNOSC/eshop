// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;

/// <summary>
/// A single address on a customer's profile, as listed (read-only) by
/// <c>AdminCustomerDetailPage</c>'s addresses grid, nested under <see cref="CustomerDetailViewModel"/>.
/// Disambiguated from the existing editable <see cref="CustomerAddressViewModel"/> (the add-address
/// form's ViewModel) — read-only display data mapped from
/// <see cref="Tnosc.EShop.Client.Web.Contracts.Identity.CustomerAddress"/> by
/// <c>AdminCustomerDetailService</c>, so it carries no DataAnnotations.
/// </summary>
public sealed class CustomerAddressListItemViewModel
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
