// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Serialization;

/// <summary>
/// Reads and writes a strongly-typed entity id as its underlying primitive, so a persisted payload
/// carries <c>"019a…"</c> rather than <c>{"Value":"019a…"}</c> and can be read back again.
/// </summary>
/// <remarks>
/// Reconstruction goes through <see cref="IEntityId{TSelf, TValue}.From"/> — the static abstract
/// member that exists for exactly this. Without it these ids are write-only to
/// <see cref="JsonSerializer"/>: a typed id declares only a non-public constructor and a get-only
/// <c>Value</c>, so the serializer can emit one but has no way to build one back.
/// </remarks>
/// <typeparam name="TId">The strongly-typed id.</typeparam>
/// <typeparam name="TValue">The id's underlying primitive.</typeparam>
internal sealed class EntityIdJsonConverter<TId, TValue> : JsonConverter<TId>
    where TId : class, IEntityId<TId, TValue>
    where TValue : notnull
{
    /// <inheritdoc />
    public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        TValue? value = JsonSerializer.Deserialize<TValue>(reader: ref reader, options: options);

        return value is null ? null : TId.From(value: value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(argument: value);

        JsonSerializer.Serialize(writer: writer, value: value.Value, options: options);
    }
}
