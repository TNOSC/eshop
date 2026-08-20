// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Basket.Baskets;

/// <summary>
/// Every way a candidate <see cref="Quantity"/> can break its invariant.
/// </summary>
public static class QuantityErrors
{
    /// <summary>
    /// Gets the error returned when a quantity falls outside <see cref="Quantity.MinValue"/>..<see cref="Quantity.MaxValue"/>.
    /// </summary>
    public static Error OutOfRange => Error.Validation(
        code: "Quantity.OutOfRange",
        description: $"A quantity must be between {Quantity.MinValue} and {Quantity.MaxValue}.");
}
