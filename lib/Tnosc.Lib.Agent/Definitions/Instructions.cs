// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.ValueObjects;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// The system prompt that defines an agent's role, tone and boundaries.
/// </summary>
/// <remarks>
/// Emptiness is the invariant worth enforcing: an agent with no instructions still runs, still
/// answers, and still calls tools — it simply does so with whatever default persona the model
/// happens to have. That failure is silent and only shows up as bad answers in production, which is
/// exactly the kind of thing a value object should make impossible to construct.
/// </remarks>
public sealed record Instructions : ValueObject
{
    /// <summary>
    /// The maximum number of characters an instruction text may contain.
    /// </summary>
    /// <remarks>
    /// A generous ceiling that exists to catch an accidentally embedded document, not to budget
    /// tokens — the model's own context window is the real constraint.
    /// </remarks>
    public const int MaxLength = 32_000;

    private Instructions(string value) => Value = value;

    /// <summary>
    /// Gets the instruction text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates an <see cref="Instructions"/> from its text form.
    /// </summary>
    /// <param name="value">The candidate instruction text.</param>
    /// <returns>
    /// The created <see cref="Instructions"/>, or <c>Instructions.Empty</c> /
    /// <c>Instructions.TooLong</c> when <paramref name="value"/> breaks the invariant.
    /// </returns>
    public static Result<Instructions> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return InstructionsErrors.Empty;
        }

        if (value.Length > MaxLength)
        {
            return InstructionsErrors.TooLong;
        }

        return new Instructions(value: value);
    }

    /// <summary>
    /// Returns the instruction text.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() => Value;
}
