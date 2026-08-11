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
using System.Reflection;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// A <see cref="FrozenDictionary{TKey,TValue}"/>-backed, bidirectional implementation of
/// <see cref="IDomainEventTypeRegistry"/>, built once at startup by scanning the supplied
/// assemblies for concrete <see cref="IDomainEvent"/> implementors.
/// </summary>
internal sealed class DomainEventTypeRegistry : IDomainEventTypeRegistry
{
    private readonly FrozenDictionary<Type, string> _namesByType;
    private readonly FrozenDictionary<string, Type> _typesByName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventTypeRegistry"/> class, scanning the
    /// specified assemblies for concrete <see cref="IDomainEvent"/> implementors.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for domain event types.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="assemblies"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when two domain event types resolve to the same contract name.
    /// </exception>
    public DomainEventTypeRegistry(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(argument: assemblies);

        IEnumerable<Type> domainEventTypes = assemblies
            .SelectMany(selector: assembly => assembly.GetTypes())
            .Where(predicate: type => type is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(c: type));

        var namesByType = new Dictionary<Type, string>();
        var typesByName = new Dictionary<string, Type>(comparer: StringComparer.Ordinal);

        foreach (Type domainEventType in domainEventTypes)
        {
            string name = domainEventType.GetCustomAttribute<DomainEventNameAttribute>()?.Name ?? domainEventType.Name;

            if (typesByName.TryGetValue(key: name, value: out Type? existing))
            {
                throw new InvalidOperationException(
                    message: $"Domain event contract name '{name}' is registered by both '{existing}' and '{domainEventType}'. " +
                    "Give one of them a distinct [DomainEventName].");
            }

            namesByType.Add(key: domainEventType, value: name);
            typesByName.Add(key: name, value: domainEventType);
        }

        _namesByType = namesByType.ToFrozenDictionary();
        _typesByName = typesByName.ToFrozenDictionary(comparer: StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public string GetName(Type domainEventType)
    {
        ArgumentNullException.ThrowIfNull(argument: domainEventType);

        return _namesByType.TryGetValue(key: domainEventType, value: out string? name)
            ? name
            : throw new InvalidOperationException(message: $"Domain event type '{domainEventType}' is not registered.");
    }

    /// <inheritdoc />
    public bool TryResolve(string name, [NotNullWhen(true)] out Type? domainEventType) =>
        _typesByName.TryGetValue(key: name, value: out domainEventType);
}
