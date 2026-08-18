// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Components;

/// <summary>
/// The delivery-address block shared by <c>CheckoutPage</c> and <c>OrderDetailPage</c> — the two
/// pages had no common source type for an address (a <c>CustomerAddress</c> versus an order's own
/// flat shipping fields), so this takes plain strings rather than forcing one.
/// </summary>
public partial class DeliveryAddress : ComponentBase
{
    /// <summary>Gets or sets the street line.</summary>
    [Parameter]
    [EditorRequired]
    public string Street { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    [Parameter]
    [EditorRequired]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    [Parameter]
    [EditorRequired]
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the country.</summary>
    [Parameter]
    [EditorRequired]
    public string Country { get; set; } = string.Empty;
}
