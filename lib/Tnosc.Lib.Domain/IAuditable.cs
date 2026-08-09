// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Domain;

/// <summary>
/// Defines a contract for auditable entities that track creation and modification metadata.
/// </summary>
/// <remarks>
/// Implement this interface on domain entities to automatically capture audit information
/// such as who created or updated the entity and when those actions occurred.
/// This is commonly used in persistence layers to enforce consistency and accountability.
/// </remarks>
public interface IAuditable
{
    /// <summary>
    /// Gets the date and time when the entity was created.
    /// </summary>
    /// <value>
    /// A <see cref="DateTime"/> representing the creation timestamp.
    /// </value>
    DateTime CreatedOnUtc { get; }

    /// <summary>
    /// Gets the date and time when the entity was last updated.
    /// </summary>
    /// <value>
    /// A <see cref="DateTime"/> representing the last modification timestamp.
    /// </value>
    DateTime UpdatedOnUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who created the entity.
    /// </summary>
    string CreatedBy { get; }

    /// <summary>
    /// Gets the identifier of the user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; }
}
