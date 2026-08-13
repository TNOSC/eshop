// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Every way a candidate <see cref="PersonName"/> can break its invariant.
/// </summary>
public static class PersonNameErrors
{
    /// <summary>
    /// Gets the error returned when no first name was supplied.
    /// </summary>
    public static Error FirstNameRequired => Error.Validation(
        code: "PersonName.FirstNameRequired",
        description: "A first name is required.");

    /// <summary>
    /// Gets the error returned when no last name was supplied.
    /// </summary>
    public static Error LastNameRequired => Error.Validation(
        code: "PersonName.LastNameRequired",
        description: "A last name is required.");

    /// <summary>
    /// Gets the error returned when a first name exceeds <see cref="PersonName.MaxPartLength"/>.
    /// </summary>
    public static Error FirstNameTooLong => Error.Validation(
        code: "PersonName.FirstNameTooLong",
        description: $"A first name must be at most {PersonName.MaxPartLength} characters long.");

    /// <summary>
    /// Gets the error returned when a last name exceeds <see cref="PersonName.MaxPartLength"/>.
    /// </summary>
    public static Error LastNameTooLong => Error.Validation(
        code: "PersonName.LastNameTooLong",
        description: $"A last name must be at most {PersonName.MaxPartLength} characters long.");
}
