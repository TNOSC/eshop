// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Infrastructure.Persistence.Conventions;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.Lib.Infrastructure.Persistence.Contexts;

/// <summary>
/// Builds the write and read halves of the EF Core model from a single configuration assembly, and
/// applies the model-wide rules shared by both <see cref="WriteDbContextBase"/> and
/// <see cref="ReadDbContextBase"/>.
/// </summary>
internal static class ModelComposition
{
    /// <summary>
    /// Applies every <see cref="IEntityTypeConfiguration{TEntity}"/> in <paramref name="assembly"/> whose
    /// entity type implements <see cref="IAggregateRoot"/>, then the shared model rules.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="assembly">The assembly to scan for write-side configurations.</param>
    public static void ApplyWriteModel(ModelBuilder modelBuilder, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(assembly, ConfiguresEntity<IAggregateRoot>);
        ApplySharedModelRules(modelBuilder);
    }

    /// <summary>
    /// Applies every <see cref="IEntityTypeConfiguration{TEntity}"/> in <paramref name="assembly"/> whose
    /// entity type implements <see cref="IReadModel"/>, then the shared model rules.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="assembly">The assembly to scan for read-side configurations.</param>
    public static void ApplyReadModel(ModelBuilder modelBuilder, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(assembly, ConfiguresEntity<IReadModel>);
        ApplySharedModelRules(modelBuilder);
    }

    /// <summary>
    /// Applies the strongly-typed id value conversions to every id type discovered in
    /// <paramref name="assembly"/>. Shared by both context bases so ids convert identically on
    /// the write and read sides.
    /// </summary>
    /// <param name="configurationBuilder">The model configuration builder to configure.</param>
    /// <param name="assembly">The assembly to scan for <see cref="IEntityId{TSelf, TValue}"/> implementations.</param>
    public static void ApplySharedConventions(ModelConfigurationBuilder configurationBuilder, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(assembly);

        configurationBuilder.ApplyEntityIdConversions(assembly);
    }

    /// <summary>
    /// Determines whether <paramref name="configurationType"/> is a closed
    /// <see cref="IEntityTypeConfiguration{TEntity}"/> whose entity type argument implements
    /// <typeparamref name="TMarker"/>.
    /// </summary>
    /// <typeparam name="TMarker">The marker interface selecting either the write or read side.</typeparam>
    /// <param name="configurationType">A candidate configuration type discovered by assembly scanning.</param>
    /// <returns><see langword="true"/> if the configuration targets an entity implementing <typeparamref name="TMarker"/>.</returns>
    private static bool ConfiguresEntity<TMarker>(Type configurationType)
    {
        Type? entityType = configurationType
            .GetInterfaces()
            .Where(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
            .Select(static i => i.GetGenericArguments()[0])
            .FirstOrDefault();

        return entityType is not null && typeof(TMarker).IsAssignableFrom(entityType);
    }

    /// <summary>
    /// Applies the model rules common to both the write and read models: strongly-typed id key
    /// properties never generate a database value, and <c>AggregateRoot.Version</c> is the
    /// optimistic-concurrency token where present.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    private static void ApplySharedModelRules(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty keyProperty in entityType.FindPrimaryKey()?.Properties ?? [])
            {
                if (typeof(IEntityId).IsAssignableFrom(keyProperty.ClrType))
                {
                    keyProperty.ValueGenerated = ValueGenerated.Never;
                }
            }

            if (typeof(IAggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                IMutableProperty? versionProperty = entityType.FindProperty(nameof(AggregateRoot<IEntityId>.Version));
                if (versionProperty is { } property)
                {
                    property.IsConcurrencyToken = true;
                }
            }
        }
    }
}
