// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// EF Core mapping for <see cref="TestAggregate"/>, applied only by <see cref="TestModelCustomizer"/>
/// inside the integration test fixture — never by the production model.
/// </summary>
/// <remarks>
/// <see cref="TableName"/> lives in the <c>catalog</c> schema so <c>Respawn</c> truncates it between
/// tests without any change to <see cref="PostgresFixture"/>'s schema list, and
/// <see cref="PostgresFixture"/> creates the physical table with raw SQL since no EF migration
/// describes it. Both audit columns force <see cref="DateTimeKind.Utc"/> through a value converter:
/// a freshly-created aggregate never goes through the <c>Modified</c> audit path, so
/// <c>UpdatedOnUtc</c> is written with its CLR default, <see cref="DateTime.MinValue"/> (kind
/// <see cref="DateTimeKind.Unspecified"/>) — Npgsql rejects <c>Unspecified</c>-kind values for
/// <c>timestamp with time zone</c> columns, and that rejection would be an artifact of this test
/// model, not a signal about the B3 fix under test.
/// </remarks>
internal sealed class TestAggregateConfiguration : IEntityTypeConfiguration<TestAggregate>
{
    /// <summary>
    /// The schema the test table lives in — reuses <c>catalog</c> so it is truncated by the same
    /// <c>Respawn</c> configuration as every other integration test table.
    /// </summary>
    public const string SchemaName = "catalog";

    /// <summary>
    /// The name of the test table.
    /// </summary>
    public const string TableName = "test_aggregates";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TestAggregate> builder)
    {
        builder.ToTable(TableName, SchemaName);
        builder.HasKey(aggregate => aggregate.Id);

        builder.Property(aggregate => aggregate.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TestAggregateId.From(value))
            .ValueGeneratedNever();

        builder.Property(aggregate => aggregate.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(aggregate => aggregate.Version)
            .HasColumnName("version");

        builder.Property(aggregate => aggregate.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .HasColumnType("timestamp with time zone")
            .HasConversion(value => ForceUtcKind(value), value => value);

        builder.Property(aggregate => aggregate.UpdatedOnUtc)
            .HasColumnName("updated_on_utc")
            .HasColumnType("timestamp with time zone")
            .HasConversion(value => ForceUtcKind(value), value => value);

        builder.Property(aggregate => aggregate.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(200);

        builder.Property(aggregate => aggregate.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(200);

        builder.Ignore(aggregate => aggregate.DomainEvents);
    }

    // DateTime.MinValue — UpdatedOnUtc's unset default — has DateTimeKind.Unspecified, which Npgsql
    // rejects for "timestamp with time zone". Every value this test model ever produces is UTC by
    // convention (audit-stamped via TimeProvider.GetUtcNow().UtcDateTime), so forcing the Kind here
    // is a safe, test-only workaround, not a behavioural change.
    private static DateTime ForceUtcKind(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
