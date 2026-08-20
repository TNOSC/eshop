// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Every way a candidate <see cref="Email"/> can break its format invariant.
/// </summary>
public static class EmailErrors
{
    /// <summary>
    /// Gets the error returned when no email address was supplied.
    /// </summary>
    public static Error Empty => Error.Validation(
        code: "Email.Empty",
        description: "An email address is required.");

    /// <summary>
    /// Gets the error returned when an email address exceeds <see cref="Email.MaxLength"/>.
    /// </summary>
    public static Error TooLong => Error.Validation(
        code: "Email.TooLong",
        description: $"An email address must be at most {Email.MaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when an email address is not in a recognisable form.
    /// </summary>
    public static Error InvalidFormat => Error.Validation(
        code: "Email.InvalidFormat",
        description: "An email address must be of the form 'local@domain'.");
}
