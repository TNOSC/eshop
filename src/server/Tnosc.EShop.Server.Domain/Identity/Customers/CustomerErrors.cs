// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Every failure a caller can get back about a <see cref="Customer"/>, defined once.
/// </summary>
public static class CustomerErrors
{
    /// <summary>
    /// No customer carries the requested identifier.
    /// </summary>
    /// <param name="customerId">The identifier that was looked up.</param>
    public static Error NotFound(Guid customerId) => Error.NotFound(
        code: "Customer.NotFound",
        description: $"Customer {customerId} was not found.");

    /// <summary>
    /// No customer has been provisioned yet for the caller's identity-provider account.
    /// </summary>
    /// <remarks>
    /// The remedy is a <c>POST /api/identity/customers</c>, which the client makes after every login.
    /// </remarks>
    /// <param name="externalUserId">The subject identifier that was looked up.</param>
    public static Error NotProvisioned(string externalUserId) => Error.NotFound(
        code: "Customer.NotProvisioned",
        description: $"No customer profile exists for user '{externalUserId}'. Provision one before using this endpoint.");

    /// <summary>
    /// A different identity-provider account has already registered this email address.
    /// </summary>
    /// <param name="email">The email address that is already taken.</param>
    public static Error EmailAlreadyRegistered(string email) => Error.Conflict(
        code: "Customer.EmailAlreadyRegistered",
        description: $"A customer with email '{email}' already exists.");

    /// <summary>
    /// The customer has been deactivated and can no longer be modified.
    /// </summary>
    /// <param name="customerId">The identifier of the deactivated customer.</param>
    public static Error Deactivated(Guid customerId) => Error.Conflict(
        code: "Customer.Deactivated",
        description: $"Customer {customerId} is deactivated and can no longer be modified.");

    /// <summary>
    /// The customer has already been deactivated.
    /// </summary>
    /// <param name="customerId">The identifier of the deactivated customer.</param>
    public static Error AlreadyDeactivated(Guid customerId) => Error.Conflict(
        code: "Customer.AlreadyDeactivated",
        description: $"Customer {customerId} is already deactivated.");

    /// <summary>
    /// Gets the error returned when a customer identifier is missing.
    /// </summary>
    public static Error IdRequired => Error.Validation(
        code: "Customer.IdRequired",
        description: "A customer identifier is required.");

    /// <summary>
    /// Gets the error returned when an address identifier is missing.
    /// </summary>
    public static Error AddressIdRequired => Error.Validation(
        code: "Customer.AddressIdRequired",
        description: "An address identifier is required.");
}
