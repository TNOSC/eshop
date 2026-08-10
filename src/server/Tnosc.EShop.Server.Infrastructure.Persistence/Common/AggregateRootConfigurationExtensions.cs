// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Common;

/// <summary>
/// Maps the columns every <see cref="AggregateRoot{TEntityId}"/> inherits — the four audit columns
/// and the optimistic-concurrency version — so each aggregate's own configuration only describes what
/// is actually specific to it.
/// </summary>
internal static class AggregateRootConfigurationExtensions
{
    private const string VersionPropertyName = "Version";
    private const string DomainEventsPropertyName = "DomainEvents";
    private const string TimestampColumnType = "timestamp with time zone";

    /// <summary>
    /// Maps <c>Version</c> and the four <see cref="IAuditable"/> columns to their snake_case names, and
    /// tells EF Core to ignore the in-memory domain-event collection.
    /// </summary>
    /// <remarks>
    /// Both timestamps force <see cref="DateTimeKind.Utc"/> through a value converter. A freshly
    /// inserted aggregate never goes through the <c>Modified</c> audit path, so <c>UpdatedOnUtc</c> is
    /// written with its CLR default, <see cref="DateTime.MinValue"/>, whose <see cref="DateTime.Kind"/>
    /// is <see cref="DateTimeKind.Unspecified"/> — and Npgsql rejects <c>Unspecified</c> values for a
    /// <c>timestamp with time zone</c> column. Every value the write pipeline produces is UTC by
    /// construction (<c>TimeProvider.GetUtcNow().UtcDateTime</c>), so stamping the kind is a mapping
    /// detail, not a behavioural change.
    /// </remarks>
    /// <typeparam name="TAggregate">The aggregate root being configured.</typeparam>
    /// <param name="builder">The entity type builder to configure.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static EntityTypeBuilder<TAggregate> ConfigureAggregateRootColumns<TAggregate>(
        this EntityTypeBuilder<TAggregate> builder)
        where TAggregate : class, IAggregateRoot, IAuditable, IHasDomainEvent
    {
        builder.Property<int>(propertyName: VersionPropertyName)
            .HasColumnName(name: "version");

        builder.Property<DateTime>(propertyName: nameof(IAuditable.CreatedOnUtc))
            .HasColumnName(name: "created_on_utc")
            .HasColumnType(typeName: TimestampColumnType)
            .HasConversion(
                convertToProviderExpression: value => ForceUtcKind(value: value),
                convertFromProviderExpression: value => value);

        builder.Property<DateTime>(propertyName: nameof(IAuditable.UpdatedOnUtc))
            .HasColumnName(name: "updated_on_utc")
            .HasColumnType(typeName: TimestampColumnType)
            .HasConversion(
                convertToProviderExpression: value => ForceUtcKind(value: value),
                convertFromProviderExpression: value => value);

        builder.Property<string>(propertyName: nameof(IAuditable.CreatedBy))
            .HasColumnName(name: "created_by")
            .HasMaxLength(maxLength: 200)
            .IsRequired();

        builder.Property<string>(propertyName: nameof(IAuditable.UpdatedBy))
            .HasColumnName(name: "updated_by")
            .HasMaxLength(maxLength: 200);

        builder.Ignore(propertyName: DomainEventsPropertyName);

        return builder;
    }

    private static DateTime ForceUtcKind(DateTime value) =>
        DateTime.SpecifyKind(value: value, kind: DateTimeKind.Utc);
}
