// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Resolves the durable, versioned contract name written to <see cref="OutboxMessage.Type"/> for a
/// domain event CLR type, and resolves it back.
/// </summary>
/// <remarks>
/// The stored name is never a CLR <see cref="Type.AssemblyQualifiedName"/>: baking the assembly name
/// and version into a durable row means renaming or moving the assembly orphans every unprocessed
/// message. Instead the name is the value of <c>Tnosc.Lib.Domain.DomainEventNameAttribute.Name</c>
/// when the domain event declares it (for example, <c>"catalog.product-created.v1"</c>), falling
/// back to the CLR type's <c>Name</c> otherwise. This decouples the durable payload from CLR
/// names and gives an obvious versioning path (for example, bumping to <c>".v2"</c> on a breaking
/// change to the event's shape).
/// </remarks>
public interface IDomainEventTypeRegistry
{
    /// <summary>
    /// Gets the durable, versioned contract name for the specified domain event type.
    /// </summary>
    /// <param name="domainEventType">The domain event CLR type.</param>
    /// <returns>The registered contract name for <paramref name="domainEventType"/>.</returns>
    string GetName(Type domainEventType);

    /// <summary>
    /// Attempts to resolve the domain event CLR type registered under the specified contract name.
    /// </summary>
    /// <param name="name">The durable, versioned contract name.</param>
    /// <param name="domainEventType">
    /// When this method returns <see langword="true"/>, the resolved domain event CLR type;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was resolved; otherwise, <see langword="false"/>.</returns>
    bool TryResolve(string name, [NotNullWhen(true)] out Type? domainEventType);
}
