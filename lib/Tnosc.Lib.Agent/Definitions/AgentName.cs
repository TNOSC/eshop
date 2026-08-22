// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.ValueObjects;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// An agent's stable, host-unique name: lowercase kebab-case, used as the DI service key and as the
/// last segment of the agent's route.
/// </summary>
/// <remarks>
/// The format is not cosmetic. The same literal is written twice by two projects that cannot see
/// each other — the API layer names the agent when mapping its endpoint, and the host registers the
/// agent under that key — so it travels through a URL and through a keyed DI lookup. Restricting it
/// to URL-safe kebab-case means neither leg has to escape or normalize it.
/// </remarks>
public sealed record AgentName : ValueObject
{
    /// <summary>
    /// The maximum number of characters an agent name may contain.
    /// </summary>
    public const int MaxLength = 64;

    private AgentName(string value) => Value = value;

    /// <summary>
    /// Gets the agent name text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates an <see cref="AgentName"/> from its text form, validating the format invariant.
    /// </summary>
    /// <param name="value">The candidate name. Lowercase ASCII letters, digits and dashes only.</param>
    /// <returns>
    /// The created <see cref="AgentName"/>, or <c>AgentName.Empty</c> / <c>AgentName.TooLong</c> /
    /// <c>AgentName.InvalidFormat</c> when <paramref name="value"/> breaks the format invariant.
    /// </returns>
    public static Result<AgentName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return AgentNameErrors.Empty;
        }

        if (value.Length > MaxLength)
        {
            return AgentNameErrors.TooLong;
        }

        if (!HasValidFormat(value: value))
        {
            return AgentNameErrors.InvalidFormat;
        }

        return new AgentName(value: value);
    }

    /// <summary>
    /// Returns the agent name text.
    /// </summary>
    /// <returns>The value of <see cref="Value"/>.</returns>
    public override string ToString() => Value;

    private static bool HasValidFormat(string value)
    {
        // A leading or trailing dash would produce a double slash or an empty route segment.
        if (value[0] == '-' || value[^1] == '-')
        {
            return false;
        }

        foreach (char character in value)
        {
            bool isAllowed = char.IsAsciiLetterLower(c: character) ||
                             char.IsAsciiDigit(c: character) ||
                             character == '-';

            if (!isAllowed)
            {
                return false;
            }
        }

        return true;
    }
}

