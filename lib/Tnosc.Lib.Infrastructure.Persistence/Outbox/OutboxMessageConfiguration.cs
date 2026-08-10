// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
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
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(TableName, SchemaName);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(m => m.Type).HasColumnName("type").HasMaxLength(256).IsRequired();
        builder.Property(m => m.Content).HasColumnName("content").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OccurredOnUtc).HasColumnName("occurred_on_utc").HasColumnType("timestamp with time zone");
        builder.Property(m => m.ProcessedOnUtc).HasColumnName("processed_on_utc").HasColumnType("timestamp with time zone");
        builder.Property(m => m.Attempts).HasColumnName("attempts");
        builder.Property(m => m.NextAttemptOnUtc).HasColumnName("next_attempt_on_utc").HasColumnType("timestamp with time zone");
        builder.Property(m => m.Error).HasColumnName("error").HasMaxLength(4000);

        builder.HasIndex(m => new { m.NextAttemptOnUtc, m.OccurredOnUtc })
               .HasFilter("processed_on_utc IS NULL")
               .HasDatabaseName("ix_outbox_messages_pending");
    }
}
