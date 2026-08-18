// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.ViewModels;

/// <summary>
/// The caller's profile as loaded for checkout, shown by <c>CheckoutPage</c>. A separate type from
/// <c>Admin/Identity</c>'s customer ViewModels per the per-slice naming rule — read-only display data
/// mapped from <see cref="Tnosc.EShop.Client.Web.Contracts.Identity.Customer"/> by
/// <c>CheckoutService</c>, so it carries no DataAnnotations.
/// </summary>
public sealed class CheckoutCustomerViewModel
{
    /// <summary>Gets or sets the customer's default address id, when one is set.</summary>
    public Guid? DefaultAddressId { get; init; }

    /// <summary>Gets or sets the customer's addresses.</summary>
    public IReadOnlyList<CheckoutAddressViewModel> Addresses { get; init; } = [];
}
