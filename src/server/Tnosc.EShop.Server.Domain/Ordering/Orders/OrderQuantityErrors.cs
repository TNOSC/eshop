// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Every failure a caller can get back about an <see cref="OrderQuantity"/>, defined once.
/// </summary>
public static class OrderQuantityErrors
{
    /// <summary>
    /// Gets the error returned when a quantity falls outside the permitted range.
    /// </summary>
    public static Error OutOfRange => Error.Validation(
        code: "OrderQuantity.OutOfRange",
        description: $"An order line quantity must be between {OrderQuantity.MinValue} and {OrderQuantity.MaxValue}.");
}
