// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// Every way a candidate <see cref="Money"/> can break its invariants.
/// </summary>
public static class MoneyErrors
{
    /// <summary>
    /// Gets the error returned when an amount is below zero.
    /// </summary>
    public static Error NegativeAmount => Error.Validation(
        code: "Money.NegativeAmount",
        description: "An amount must be greater than or equal to zero.");

    /// <summary>
    /// Gets the error returned when a currency is not a three-letter uppercase ISO 4217 code.
    /// </summary>
    public static Error InvalidCurrency => Error.Validation(
        code: "Money.InvalidCurrency",
        description: "A currency must be a three-letter uppercase ISO 4217 code.");
}
