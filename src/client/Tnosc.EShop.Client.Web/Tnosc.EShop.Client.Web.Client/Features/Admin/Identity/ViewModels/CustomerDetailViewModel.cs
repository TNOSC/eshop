// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;

/// <summary>
/// A single customer's full detail, as shown by <c>AdminCustomerDetailPage</c>. Read-only display
/// data mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Identity.Customer"/> by
/// <c>AdminCustomerDetailService</c> — not a form, so it carries no DataAnnotations. The profile-edit
/// and add-address forms keep their own editable ViewModels
/// (<see cref="CustomerProfileViewModel"/>, <see cref="CustomerAddressViewModel"/>).
/// </summary>
public sealed class CustomerDetailViewModel
{
    /// <summary>Gets or sets the customer id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the customer's email.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or sets the customer's first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Gets or sets the customer's last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Gets or sets the customer's phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Gets or sets a value indicating whether the customer's profile is active.</summary>
    public bool IsActive { get; init; }

    /// <summary>Gets or sets the customer's default address id, when one is set.</summary>
    public Guid? DefaultAddressId { get; init; }

    /// <summary>Gets or sets the customer's addresses.</summary>
    public IReadOnlyList<CustomerAddressListItemViewModel> Addresses { get; init; } = [];
}
