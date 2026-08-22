// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tnosc.Lib.Domain.ValueObjects;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// The set of tool names an agent is permitted to use, as an allow-list.
/// </summary>
/// <remarks>
/// <para>
/// An allow-list rather than a deny-list, deliberately: tools are discovered at run time from a
/// tool server, so a deny-list would silently widen an agent's reach every time someone published a
/// new tool. With an allow-list, a new tool reaches an agent only when someone names it here.
/// </para>
/// <para>
/// <see cref="IsUnrestricted"/> — an empty list — means "every tool the server exposes", which is
/// the right default for a single-purpose host but is exactly what the paragraph above warns about.
/// Prefer naming the tools.
/// </para>
/// <para>
/// Backed by an <see cref="ImmutableArray{T}"/>, and equality is written out by hand. Immutability
/// alone is not enough: <see cref="ImmutableArray{T}"/> compares by the identity of its underlying
/// array, so two lists holding the same names would compare unequal and quietly break every value
/// object that contains one. The compiler-generated record equality is therefore replaced below
/// rather than trusted.
/// </para>
/// </remarks>
public sealed record ToolAllowList : ValueObject
{
    private ToolAllowList(ImmutableArray<string> names) => Names = names;

    /// <summary>
    /// Gets the permitted tool names, in the order supplied.
    /// </summary>
    public ImmutableArray<string> Names { get; }

    /// <summary>
    /// Gets a value indicating whether every tool the server exposes is permitted.
    /// </summary>
    public bool IsUnrestricted => Names.IsEmpty;

    /// <summary>
    /// Gets an allow-list permitting every tool the server exposes.
    /// </summary>
    public static ToolAllowList Unrestricted { get; } = new(names: []);

    /// <summary>
    /// Creates a <see cref="ToolAllowList"/> from a set of tool names.
    /// </summary>
    /// <param name="names">
    /// The permitted tool names. An empty sequence yields <see cref="Unrestricted"/>.
    /// </param>
    /// <returns>
    /// The created <see cref="ToolAllowList"/>, or <c>ToolAllowList.NameEmpty</c> /
    /// <c>ToolAllowList.Duplicate</c> when a name is blank or repeated.
    /// </returns>
    public static Result<ToolAllowList> Create(IEnumerable<string>? names)
    {
        if (names is null)
        {
            return Unrestricted;
        }

        List<string> collected = [.. names];

        if (collected.Exists(match: static name => string.IsNullOrWhiteSpace(value: name)))
        {
            return ToolAllowListErrors.NameEmpty;
        }

        if (collected.Distinct(comparer: StringComparer.Ordinal).Count() != collected.Count)
        {
            return ToolAllowListErrors.Duplicate;
        }

        return new ToolAllowList(names: [.. collected]);
    }

    /// <summary>
    /// Determines whether a tool is permitted by this allow-list.
    /// </summary>
    /// <param name="toolName">The tool name to test.</param>
    /// <returns>
    /// <see langword="true"/> when the list is unrestricted or names <paramref name="toolName"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public bool Permits(string toolName) =>
        IsUnrestricted || Names.Contains(value: toolName, comparer: StringComparer.Ordinal);

    /// <summary>
    /// Determines whether this allow-list holds the same names, in the same order, as another.
    /// </summary>
    /// <param name="other">The allow-list to compare against.</param>
    /// <returns><see langword="true"/> when both hold the same names.</returns>
    public bool Equals(ToolAllowList? other) =>
        other is not null && Names.SequenceEqual(second: other.Names, comparer: StringComparer.Ordinal);

    /// <summary>
    /// Returns a hash code over the names this allow-list holds.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (string name in Names)
        {
            hash.Add(value: name, comparer: StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
