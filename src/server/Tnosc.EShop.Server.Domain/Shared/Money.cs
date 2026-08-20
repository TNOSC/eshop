// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using Tnosc.Lib.Shared.Results;
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
    /// Scales this amount by a non-negative decimal factor, rounding the result to two decimal places.
    /// </summary>
    /// <remarks>
    /// The counterpart to <see cref="Multiply(int)"/> for the fractional factors a discount produces.
    /// Returns <see cref="Money"/> rather than <c>Result&lt;Money&gt;</c> because a non-negative amount
    /// scaled by a non-negative factor cannot break the "never negative" invariant, so there is no
    /// failure for a caller to handle. Rounding is banker's rounding, matching the <c>numeric(18,2)</c>
    /// column the amount is stored in.
    /// </remarks>
    /// <param name="factor">The non-negative factor to scale by — <c>0.9m</c> for a 10% discount.</param>
    /// <returns>The scaled <see cref="Money"/>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="factor"/> is negative.</exception>
    public Money Scale(decimal factor)
    {
        System.ArgumentOutOfRangeException.ThrowIfNegative(value: factor);

        return new Money(
            amount: decimal.Round(d: Amount * factor, decimals: 2, mode: System.MidpointRounding.ToEven),
            currency: Currency);
    }

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
