// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Profile.ViewModels;

/// <summary>
/// The caller's own profile, as shown by <c>MyProfilePage</c>. Read-only display data mapped from
/// <see cref="Tnosc.EShop.Client.Web.Contracts.Identity.Customer"/> by <c>MyProfileService</c> — not
/// a form, so it carries no DataAnnotations. The profile-edit and add-address forms keep their own
/// editable ViewModels (<see cref="MyProfileFormViewModel"/>, <see cref="MyAddressFormViewModel"/>).
/// </summary>
public sealed class MyProfileViewModel
{
    /// <summary>Gets or sets the customer id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the caller's email.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or sets the caller's first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Gets or sets the caller's last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Gets or sets the caller's phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Gets or sets the caller's default address id, when one is set.</summary>
    public Guid? DefaultAddressId { get; init; }

    /// <summary>Gets or sets the caller's addresses.</summary>
    public IReadOnlyList<MyAddressListItemViewModel> Addresses { get; init; } = [];
}
