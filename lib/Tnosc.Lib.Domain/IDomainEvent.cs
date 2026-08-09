// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Represents an event that has occurred within the domain, capturing the time of occurrence.
/// </summary>
/// <remarks>Domain events are used to signal significant changes or actions within the domain model.
/// Implementations typically include additional contextual information relevant to the specific event.</remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the identifier.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the date and time when the event occurred, expressed in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime OccurredOnUtc { get; }
}
