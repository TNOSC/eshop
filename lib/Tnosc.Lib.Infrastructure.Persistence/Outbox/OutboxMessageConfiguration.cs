// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Configures the EF Core mapping for <see cref="OutboxMessage"/>.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <summary>
    /// The Postgres schema that owns the outbox table.
    /// </summary>
    public const string SchemaName = "outbox";

    /// <summary>
    /// The name of the outbox table.
    /// </summary>
    public const string TableName = "outbox_messages";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: TableName, schema: SchemaName);
        builder.HasKey(keyExpression: m => m.Id);
        builder.Property(propertyExpression: m => m.Id).HasColumnName(name: "id").ValueGeneratedNever();
        builder.Property(propertyExpression: m => m.Type).HasColumnName(name: "type").HasMaxLength(maxLength: 256).IsRequired();
        builder.Property(propertyExpression: m => m.Content).HasColumnName(name: "content").HasColumnType(typeName: "jsonb").IsRequired();
        builder.Property(propertyExpression: m => m.OccurredOnUtc).HasColumnName(name: "occurred_on_utc").HasColumnType(typeName: "timestamp with time zone");
        builder.Property(propertyExpression: m => m.ProcessedOnUtc).HasColumnName(name: "processed_on_utc").HasColumnType(typeName: "timestamp with time zone");
        builder.Property(propertyExpression: m => m.Attempts).HasColumnName(name: "attempts");
        builder.Property(propertyExpression: m => m.NextAttemptOnUtc).HasColumnName(name: "next_attempt_on_utc").HasColumnType(typeName: "timestamp with time zone");
        builder.Property(propertyExpression: m => m.Error).HasColumnName(name: "error").HasMaxLength(maxLength: 4000);

        builder.HasIndex(indexExpression: m => new { m.NextAttemptOnUtc, m.OccurredOnUtc })
               .HasFilter(sql: "processed_on_utc IS NULL")
               .HasDatabaseName(name: "ix_outbox_messages_pending");
    }
}
