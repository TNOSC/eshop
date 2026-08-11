// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Maps <see cref="ProcessedEvent"/> onto the <c>outbox.processed_events</c> table.
/// </summary>
/// <remarks>
/// Deliberately in the <c>outbox</c> schema rather than <c>idempotency</c>: the inbox is the other
/// half of outbox delivery, and keeping the pair together means one schema to reset, back up and
/// reason about when investigating a delivery problem.
/// </remarks>
public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    /// <summary>
    /// The Postgres schema owning the inbox table — shared with the outbox.
    /// </summary>
    public const string SchemaName = OutboxMessageConfiguration.SchemaName;

    /// <summary>
    /// The inbox table name.
    /// </summary>
    public const string TableName = "processed_events";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: TableName, schema: SchemaName);

        // One claim per (event, handler): several handlers may consume the same event, and each gets
        // its own at-most-once guarantee.
        builder.HasKey(keyExpression: e => new { e.EventId, e.Handler });

        builder.Property(propertyExpression: e => e.EventId).HasColumnName(name: "event_id");
        builder.Property(propertyExpression: e => e.Handler).HasColumnName(name: "handler").HasMaxLength(maxLength: IdempotencyRequestConfiguration.HandlerMaxLength).IsRequired();
        builder.Property(propertyExpression: e => e.ProcessedOnUtc).HasColumnName(name: "processed_on_utc").HasColumnType(typeName: "timestamp with time zone");

        builder.HasIndex(indexExpression: e => e.ProcessedOnUtc)
               .HasDatabaseName(name: "ix_processed_events_processed_on");
    }
}
