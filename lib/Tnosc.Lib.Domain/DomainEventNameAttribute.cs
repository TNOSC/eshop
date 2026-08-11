// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Declares the durable contract name for a domain event.
/// </summary>
/// <remarks>Applied to a <see cref="DomainEvent"/> class so persisted outbox rows can reference a
/// stable string name (for example, <c>"catalog.product-created.v1"</c>) instead of a CLR type
/// name, which would break event replay across renames or assembly changes.</remarks>
/// <param name="name">The durable, versioned name of the domain event.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DomainEventNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the durable, versioned name of the domain event.
    /// </summary>
    public string Name { get; } = name;
}
