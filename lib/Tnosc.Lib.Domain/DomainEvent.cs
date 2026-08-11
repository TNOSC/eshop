// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Represents the base type for domain events within the domain model.
/// </summary>
/// <remarks>Domain events capture significant occurrences or changes within the domain that may trigger side
/// effects or be processed by event handlers. Inherit from this type to define specific domain events relevant to your
/// application's business logic.</remarks>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="occurredOnUtc">The occurred on date and time.</param>
    protected DomainEvent(Guid id, DateTime occurredOnUtc)
        : this()
    {
        Id = id;
        OccurredOnUtc = occurredOnUtc;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; private set; }
}
