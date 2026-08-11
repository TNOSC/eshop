// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Text.Json;
using Tnosc.Lib.Infrastructure.Persistence.Serialization;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Holds the JSON serializer options shared by every closed <see cref="IdempotencyStore{TContext}"/>
/// when recording and replaying command responses.
/// </summary>
/// <remarks>
/// Kept in a non-generic type so the options instance is shared across all <c>TContext</c>
/// instantiations instead of being duplicated per closed generic type.
/// </remarks>
internal static class IdempotencySerialization
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used to record and replay command responses.
    /// </summary>
    /// <remarks>
    /// Carries <see cref="EntityIdJsonConverterFactory"/> because command responses are routinely a
    /// strongly-typed id, which the serializer can otherwise write but never read back.
    /// </remarks>
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        Converters = { new EntityIdJsonConverterFactory() }
    };
}
