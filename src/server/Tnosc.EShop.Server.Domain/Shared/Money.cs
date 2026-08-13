// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using Tnosc.Lib.Domain.Results;
using Tnosc.Lib.Domain.ValueObjects;

namespace Tnosc.EShop.Server.Domain.Shared;

/// <summary>
/// A monetary amount in a single currency.
/// </summary>
/// <remarks>
/// Lives in <c>Server.Domain/Shared</c> rather than under a single bounded context because it is
/// genuinely ubiquitous language — both Catalog (a product's price) and Basket (a basket line's
/// snapshot unit price and the basket's total) need it, and Basket must not reference Catalog. Keep
/// this kernel tiny: a type only belongs here once more than one context genuinely needs the exact
/// same vocabulary.
/// </remarks>
public sealed record Money : ValueObject
{
    /// <summary>
    /// The exact number of characters an ISO 4217 currency code contains.
    /// </summary>
    public const int CurrencyLength = 3;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Gets the amount. Never negative.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the three-letter ISO 4217 currency code, uppercase.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Creates a <see cref="Money"/>, validating that the amount is not negative and that the
    /// currency is a three-letter uppercase ISO 4217 code.
    /// </summary>
    /// <param name="amount">The monetary amount. Must be greater than or equal to zero.</param>
    /// <param name="currency">The three-letter ISO 4217 currency code.</param>
    /// <returns>
    /// The created <see cref="Money"/>, or a <c>Money.NegativeAmount</c> / <c>Money.InvalidCurrency</c>
    /// validation error.
    /// </returns>
    public static Result<Money> Create(decimal amount, string? currency)
    {
        if (amount < 0)
        {
            return MoneyErrors.NegativeAmount;
        }

        if (!IsValidCurrency(currency: currency))
        {
            return MoneyErrors.InvalidCurrency;
        }

        return new Money(amount: amount, currency: currency!);
    }

    /// <summary>
    /// Adds another <see cref="Money"/> of the same currency.
    /// </summary>
    /// <param name="other">The amount to add. Must carry the same currency.</param>
    /// <returns>The summed <see cref="Money"/>, or a <c>Money.CurrencyMismatch</c> validation error.</returns>
    public Result<Money> Add(Money other)
    {
        if (!string.Equals(a: Currency, b: other.Currency, comparisonType: System.StringComparison.Ordinal))
        {
            return MoneyErrors.CurrencyMismatch;
        }

        return new Money(amount: Amount + other.Amount, currency: Currency);
    }

    /// <summary>
    /// Multiplies this amount by a non-negative integer factor.
    /// </summary>
    /// <param name="factor">The factor to multiply by.</param>
    /// <returns>The scaled <see cref="Money"/>.</returns>
    public Money Multiply(int factor) =>
        new(amount: Amount * factor, currency: Currency);

    /// <summary>
    /// Returns the amount and currency in a human-readable form.
    /// </summary>
    /// <returns>The amount followed by the currency code.</returns>
    public override string ToString() =>
        string.Create(provider: CultureInfo.InvariantCulture, handler: $"{Amount} {Currency}");

    private static bool IsValidCurrency(string? currency)
    {
        if (currency is null || currency.Length != CurrencyLength)
        {
            return false;
        }

        foreach (char character in currency)
        {
            if (!char.IsAsciiLetterUpper(c: character))
            {
                return false;
            }
        }

        return true;
    }
}
