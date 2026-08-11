// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;

namespace Tnosc.Lib.Infrastructure.Persistence.DeadLetters;

/// <summary>
/// Maps <see cref="DeadLetterMessage"/> onto the <c>outbox.dead_letters</c> table.
/// </summary>
/// <remarks>
/// In the <c>outbox</c> schema because it is the other end of outbox delivery: everything an
/// operator needs when investigating why an event did not take effect lives in one schema.
/// </remarks>
public sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    /// <summary>
    /// The Postgres schema owning the dead-letter table — shared with the outbox.
    /// </summary>
    public const string SchemaName = OutboxMessageConfiguration.SchemaName;

    /// <summary>
    /// The dead-letter table name.
    /// </summary>
    public const string TableName = "dead_letters";

    /// <summary>
    /// The maximum length of a handler's full type name, matching the inbox's column so the same
    /// name always fits in both.
    /// </summary>
    public const int HandlerMaxLength = Idempotency.IdempotencyRequestConfiguration.HandlerMaxLength;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: TableName, schema: SchemaName);
        builder.HasKey(keyExpression: m => m.Id);

        builder.Property(propertyExpression: m => m.Id).HasColumnName(name: "id").ValueGeneratedNever();
        builder.Property(propertyExpression: m => m.OutboxMessageId).HasColumnName(name: "outbox_message_id");
        builder.Property(propertyExpression: m => m.Handler).HasColumnName(name: "handler").HasMaxLength(maxLength: HandlerMaxLength);
        builder.Property(propertyExpression: m => m.Type).HasColumnName(name: "type").HasMaxLength(maxLength: 256).IsRequired();
        builder.Property(propertyExpression: m => m.Content).HasColumnName(name: "content").HasColumnType(typeName: "jsonb").IsRequired();
        builder.Property(propertyExpression: m => m.OccurredOnUtc).HasColumnName(name: "occurred_on_utc").HasColumnType(typeName: "timestamp with time zone");
        builder.Property(propertyExpression: m => m.DeadLetteredOnUtc).HasColumnName(name: "dead_lettered_on_utc").HasColumnType(typeName: "timestamp with time zone");
        builder.Property(propertyExpression: m => m.Attempts).HasColumnName(name: "attempts");
        builder.Property(propertyExpression: m => m.Error).HasColumnName(name: "error").HasMaxLength(maxLength: 4000).IsRequired();
        builder.Property(propertyExpression: m => m.ReplayCount).HasColumnName(name: "replay_count");
        builder.Property(propertyExpression: m => m.LastReplayedOnUtc).HasColumnName(name: "last_replayed_on_utc").HasColumnType(typeName: "timestamp with time zone");
        builder.Property(propertyExpression: m => m.ReplayedOnUtc).HasColumnName(name: "replayed_on_utc").HasColumnType(typeName: "timestamp with time zone");

        // Filtered to the pending rows, which is the only listing anyone asks for: a recovered
        // message stays as an audit record but must not clutter the queue.
        builder.HasIndex(indexExpression: m => m.DeadLetteredOnUtc)
               .HasFilter(sql: "replayed_on_utc IS NULL")
               .HasDatabaseName(name: "ix_dead_letters_pending");
    }
}
