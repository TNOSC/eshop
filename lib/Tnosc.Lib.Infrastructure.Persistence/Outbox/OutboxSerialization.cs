// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Text.Json;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Holds the JSON serializer options shared by every closed <see cref="UnitOfWork{TContext}"/> when
/// converting domain events into outbox payloads.
/// </summary>
/// <remarks>
/// Kept in a non-generic type so the options instance is shared across all <c>TContext</c>
/// instantiations instead of being duplicated per closed generic type.
/// </remarks>
internal static class OutboxSerialization
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used to serialize domain events into outbox payloads.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };
}
