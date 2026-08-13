// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Every way a candidate <see cref="ExternalUserId"/> can break its invariant.
/// </summary>
public static class ExternalUserIdErrors
{
    /// <summary>
    /// Gets the error returned when no external user identifier was supplied.
    /// </summary>
    public static Error Empty => Error.Validation(
        code: "ExternalUserId.Empty",
        description: "An external user identifier is required.");

    /// <summary>
    /// Gets the error returned when an external user identifier exceeds <see cref="ExternalUserId.MaxLength"/>.
    /// </summary>
    public static Error TooLong => Error.Validation(
        code: "ExternalUserId.TooLong",
        description: $"An external user identifier must be at most {ExternalUserId.MaxLength} characters long.");
}
