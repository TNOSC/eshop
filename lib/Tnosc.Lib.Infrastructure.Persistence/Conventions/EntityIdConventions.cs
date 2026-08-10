// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Persistence.Conventions;

/// <summary>
/// Registers <see cref="EntityIdConverter{TId, TValue}"/> for every strongly-typed entity id discovered
/// in a set of assemblies, as pre-convention model configuration.
/// </summary>
/// <remarks>
/// Caveat: EF Core value converters apply only to LINQ-translated queries. Raw SQL and Dapper never
/// invoke them, so DTOs materialized from raw SQL must declare the underlying primitive (for example
/// <see cref="Guid"/>), never the strongly-typed id, wherever raw SQL is still used.
/// </remarks>
public static class EntityIdConventions
{
    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete types implementing <see cref="IEntityId{TSelf, TValue}"/>
    /// and registers an <see cref="EntityIdConverter{TId, TValue}"/> for each one, closed over that id's own
    /// type arguments.
    /// </summary>
    /// <remarks>
    /// Registering the conversion via <see cref="ModelConfigurationBuilder.Properties(Type)"/> — pre-convention
    /// model configuration — is deliberate: it applies to every property of that id type, including foreign
    /// keys EF Core introduces as shadow properties, with no per-configuration <c>HasConversion</c> call that
    /// could be forgotten on one FK. This reflection runs once per process, at model build; never on the query path.
    /// </remarks>
    /// <param name="configurationBuilder">The model configuration builder to configure.</param>
    /// <param name="assemblies">The assemblies to scan for strongly-typed entity ids.</param>
    /// <returns>The same <paramref name="configurationBuilder"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configurationBuilder"/> or <paramref name="assemblies"/> is <see langword="null"/>.</exception>
    public static ModelConfigurationBuilder ApplyEntityIdConversions(
        this ModelConfigurationBuilder configurationBuilder,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(argument: configurationBuilder);
        ArgumentNullException.ThrowIfNull(argument: assemblies);

        IEnumerable<Type> idTypes = assemblies
            .SelectMany(selector: static a => a.GetTypes())
            .Where(predicate: static t => t is { IsClass: true, IsAbstract: false });

        foreach (Type idType in idTypes)
        {
            Type? closedInterface = idType.GetInterfaces()
                .FirstOrDefault(predicate: i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IEntityId<,>)
                    && i.GetGenericArguments()[0] == idType);

            if (closedInterface is null)
            {
                continue;
            }

            Type valueType = closedInterface.GetGenericArguments()[1];
            Type converterType = typeof(EntityIdConverter<,>).MakeGenericType(idType, valueType);

            configurationBuilder.Properties(propertyType: idType).HaveConversion(conversionType: converterType);
        }

        return configurationBuilder;
    }
}
