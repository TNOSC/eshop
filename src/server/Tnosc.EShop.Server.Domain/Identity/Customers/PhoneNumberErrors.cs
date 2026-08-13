// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Every way a candidate <see cref="PhoneNumber"/> can break its format invariant.
/// </summary>
public static class PhoneNumberErrors
{
    /// <summary>
    /// Gets the error returned when no phone number was supplied to a call that requires one.
    /// </summary>
    /// <remarks>
    /// A customer's phone number is optional, so this is never produced by omitting it — callers pass
    /// <see langword="null"/> for "no number". It guards a value that was supplied but blank.
    /// </remarks>
    public static Error Empty => Error.Validation(
        code: "PhoneNumber.Empty",
        description: "A phone number is required when one is supplied.");

    /// <summary>
    /// Gets the error returned when a phone number is not in E.164 form.
    /// </summary>
    public static Error InvalidFormat => Error.Validation(
        code: "PhoneNumber.InvalidFormat",
        description: $"A phone number must be in E.164 form: a leading '+' followed by {PhoneNumber.MinDigits} to {PhoneNumber.MaxDigits} digits.");
}
