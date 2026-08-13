// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.ReadModels;

/// <summary>
/// The query-side view of <c>identity.customers</c>: flat primitives, no typed ids, no value objects.
/// </summary>
internal sealed class CustomerReadModel : IReadModel
{
    /// <summary>
    /// Gets the customer's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the identity provider's subject identifier for the customer.
    /// </summary>
    public string ExternalUserId { get; init; } = null!;

    /// <summary>
    /// Gets the customer's email address.
    /// </summary>
    public string Email { get; init; } = null!;

    /// <summary>
    /// Gets the customer's given name.
    /// </summary>
    public string FirstName { get; init; } = null!;

    /// <summary>
    /// Gets the customer's family name.
    /// </summary>
    public string LastName { get; init; } = null!;

    /// <summary>
    /// Gets the customer's contact number, if they have supplied one.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets a value indicating whether the customer is still active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets the identifier of the customer's default address, if they hold any.
    /// </summary>
    public Guid? DefaultAddressId { get; init; }

    /// <summary>
    /// Gets the customer's addresses.
    /// </summary>
    /// <remarks>
    /// A navigation on the read side, unlike the write side where addresses are an owned collection.
    /// Both map the same two tables; this shape exists so a projection can reach the addresses in one
    /// query rather than a second round trip.
    /// </remarks>
    public ICollection<CustomerAddressReadModel> Addresses { get; init; } = [];
}
