// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Serialization;

/// <summary>
/// Supplies an <see cref="EntityIdJsonConverter{TId, TValue}"/> for any type that implements
/// <see cref="IEntityId{TSelf, TValue}"/> closed over itself.
/// </summary>
/// <remarks>
/// The same discovery predicate as
/// <see cref="Conventions.EntityIdConventions.ApplyEntityIdConversions"/> uses for EF Core value
/// converters, applied to JSON instead: one registration covers every strongly-typed id in the
/// solution, present and future, with nothing to remember per id.
/// </remarks>
internal sealed class EntityIdJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        SelfClosedInterface(type: typeToConvert) is not null;

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type? closedInterface = SelfClosedInterface(type: typeToConvert);

        if (closedInterface is null)
        {
            return null;
        }

        Type valueType = closedInterface.GetGenericArguments()[1];
        Type converterType = typeof(EntityIdJsonConverter<,>).MakeGenericType(typeToConvert, valueType);

        return (JsonConverter?)Activator.CreateInstance(type: converterType);
    }

    /// <summary>
    /// Finds <paramref name="type"/>'s <see cref="IEntityId{TSelf, TValue}"/> interface closed over
    /// <paramref name="type"/> itself, which is what makes <c>From</c> return the right type.
    /// </summary>
    /// <param name="type">The candidate id type.</param>
    /// <returns>The closed interface, or <see langword="null"/> when the type is not a strongly-typed id.</returns>
    private static Type? SelfClosedInterface(Type type) =>
        type is { IsClass: true, IsAbstract: false }
            ? type.GetInterfaces().FirstOrDefault(predicate: i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IEntityId<,>)
                && i.GetGenericArguments()[0] == type)
            : null;
}
