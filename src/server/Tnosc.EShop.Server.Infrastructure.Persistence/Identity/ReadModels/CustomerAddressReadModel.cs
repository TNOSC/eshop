// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.ReadModels;

/// <summary>
/// The query-side view of <c>identity.customer_addresses</c>: flat primitives, no typed ids.
/// </summary>
internal sealed class CustomerAddressReadModel : IReadModel
{
    /// <summary>
    /// Gets the address's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the identifier of the customer the address belongs to.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Gets the street line.
    /// </summary>
    public string Street { get; init; } = null!;

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; init; } = null!;

    /// <summary>
    /// Gets the postal code.
    /// </summary>
    public string PostalCode { get; init; } = null!;

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 country code.
    /// </summary>
    public string Country { get; init; } = null!;
}
