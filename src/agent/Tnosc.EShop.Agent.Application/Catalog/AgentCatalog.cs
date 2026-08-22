// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tnosc.Lib.Agent.Definitions;

namespace Tnosc.EShop.Agent.Application.Catalog;

/// <summary>
/// The agent catalogue, built once from every discovered definition provider.
/// </summary>
/// <remarks>
/// Frozen at construction because the set of agents a host serves cannot change while it is running:
/// definitions are compiled-in data, so a dictionary that is built once and never written to is both
/// the fastest option and the one that cannot be corrupted by a concurrent reader.
/// </remarks>
internal sealed class AgentCatalog : IAgentCatalog
{
    private readonly FrozenDictionary<string, AgentDefinition> _definitions;

    /// <summary>
    /// Builds the catalogue from every discovered provider.
    /// </summary>
    /// <param name="providers">The discovered agent definition providers.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when two agents share a name. This is deliberately fatal at startup: a duplicate means
    /// one of the two would silently never be reachable, and finding that out from a 404 later is far
    /// more expensive than refusing to start now.
    /// </exception>
    public AgentCatalog(IEnumerable<IAgentDefinitionProvider> providers)
    {
        List<AgentDefinition> definitions = [.. providers.Select(selector: static provider => provider.Definition)];

        string? duplicate = definitions
            .GroupBy(keySelector: static definition => definition.Name.Value, comparer: StringComparer.Ordinal)
            .FirstOrDefault(predicate: static group => group.Count() > 1)?
            .Key;

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                message: $"More than one agent is named '{duplicate}'. Agent names must be unique within a host.");
        }

        _definitions = definitions.ToFrozenDictionary(
            keySelector: static definition => definition.Name.Value,
            comparer: StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<AgentDefinition> All => _definitions.Values;

    /// <inheritdoc />
    public bool TryGet(string agentName, [NotNullWhen(returnValue: true)] out AgentDefinition? definition) =>
        _definitions.TryGetValue(key: agentName, value: out definition);
}
