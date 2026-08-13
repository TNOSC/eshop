// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// Every failure a caller can get back about an <see cref="OrderNumber"/>, defined once.
/// </summary>
public static class OrderNumberErrors
{
    /// <summary>
    /// Gets the error returned when an order number is missing.
    /// </summary>
    public static Error Required => Error.Validation(
        code: "OrderNumber.Required",
        description: "An order number is required.");

    /// <summary>
    /// Gets the error returned when an order number does not match <c>ORD-yyyyMMdd-XXXXXX</c>.
    /// </summary>
    public static Error InvalidFormat => Error.Validation(
        code: "OrderNumber.InvalidFormat",
        description: $"An order number must be {OrderNumber.Length} characters shaped '{OrderNumber.Prefix}-yyyyMMdd-XXXXXX'.");
}
