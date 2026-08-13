// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Strongly-typed identifier for <see cref="Customer"/>.
/// </summary>
/// <remarks>
/// Deliberately this codebase's own identifier, not the Keycloak <c>sub</c>. The two are related by
/// <see cref="ExternalUserId"/>: swapping identity providers would change every external id, and
/// nothing that references a customer should have to change with it.
/// </remarks>
public sealed record CustomerId : GuidEntityId, IEntityId<CustomerId, Guid>
{
    private CustomerId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new identifier backed by a time-ordered (version 7) <see cref="Guid"/>, so inserts
    /// stay sequential and the primary-key index does not fragment.
    /// </summary>
    /// <returns>A new <see cref="CustomerId"/>.</returns>
    public static CustomerId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static CustomerId From(Guid value) => new(value);
}
