// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Payment.Payments;

/// <summary>
/// Strongly-typed identifier for <see cref="Payment"/>.
/// </summary>
public sealed record PaymentId : GuidEntityId, IEntityId<PaymentId, Guid>
{
    private PaymentId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new identifier backed by a time-ordered (version 7) <see cref="Guid"/>, so inserts
    /// stay sequential and the primary-key index does not fragment.
    /// </summary>
    /// <returns>A new <see cref="PaymentId"/>.</returns>
    public static PaymentId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static PaymentId From(Guid value) => new(value);
}
