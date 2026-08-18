// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;

/// <summary>The admin customer-detail page's profile-edit form ViewModel.</summary>
public sealed class CustomerProfileViewModel
{
    /// <summary>Gets or sets the customer's first name.</summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer's last name.</summary>
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets the customer's phone number.</summary>
    public string? PhoneNumber { get; set; }
}
