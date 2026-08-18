// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;

/// <summary>The admin customer-detail page's add-address form ViewModel.</summary>
public sealed class CustomerAddressViewModel
{
    /// <summary>Gets or sets the street line.</summary>
    [Required]
    public string Street { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    [Required]
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    [Required]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the country, as an ISO 3166-1 alpha-2 code.</summary>
    [Required]
    public string Country { get; set; } = string.Empty;
}
