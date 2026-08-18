// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;

/// <summary>
/// A single customer row in the admin customer grid, bound by <c>AdminCustomersPage</c>'s
/// <c>FluentDataGrid</c>. Read-only display data mapped from
/// <see cref="Tnosc.EShop.Client.Web.Contracts.Identity.CustomerSummary"/> by
/// <c>AdminCustomersService</c> — not a form, so it carries no DataAnnotations.
/// </summary>
public sealed class CustomerRowViewModel
{
    /// <summary>Gets or sets the customer id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the customer's email.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or sets the customer's first name.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Gets or sets the customer's last name.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the customer's profile is active.</summary>
    public bool IsActive { get; init; }
}
