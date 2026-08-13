// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// Strongly-typed identifier for <see cref="Address"/>.
/// </summary>
/// <remarks>
/// An address is a child <em>entity</em>, not a value object, precisely because it needs this: a
/// caller has to be able to name one address out of a customer's collection in order to update,
/// remove or default it.
/// </remarks>
public sealed record AddressId : GuidEntityId, IEntityId<AddressId, Guid>
{
    private AddressId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new identifier backed by a time-ordered (version 7) <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="AddressId"/>.</returns>
    public static AddressId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static AddressId From(Guid value) => new(value);
}
