// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Maps <see cref="IdempotencyRequest"/> onto the <c>idempotency.requests</c> table.
/// </summary>
public sealed class IdempotencyRequestConfiguration : IEntityTypeConfiguration<IdempotencyRequest>
{
    /// <summary>
    /// The Postgres schema owning the idempotency table.
    /// </summary>
    public const string SchemaName = "idempotency";

    /// <summary>
    /// The idempotency table name.
    /// </summary>
    public const string TableName = "requests";

    /// <summary>
    /// The maximum length of a caller-supplied idempotency key.
    /// </summary>
    public const int KeyMaxLength = 128;

    /// <summary>
    /// The maximum length of a handler's full type name.
    /// </summary>
    public const int HandlerMaxLength = 512;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: TableName, schema: SchemaName);

        // Composite key: a caller's key is scoped to one handler, so two endpoints given the same
        // key by the same client never collide — and the unique index is what serialises concurrent
        // duplicates of one key against each other.
        builder.HasKey(keyExpression: r => new { r.Key, r.Handler });

        builder.Property(propertyExpression: r => r.Key).HasColumnName(name: "idempotency_key").HasMaxLength(maxLength: KeyMaxLength).IsRequired();
        builder.Property(propertyExpression: r => r.Handler).HasColumnName(name: "handler").HasMaxLength(maxLength: HandlerMaxLength).IsRequired();
        builder.Property(propertyExpression: r => r.RequestHash).HasColumnName(name: "request_hash").HasMaxLength(maxLength: 64).IsFixedLength().IsRequired();
        builder.Property(propertyExpression: r => r.Response).HasColumnName(name: "response").HasColumnType(typeName: "jsonb");
        builder.Property(propertyExpression: r => r.ResponseType).HasColumnName(name: "response_type").HasMaxLength(maxLength: HandlerMaxLength);
        builder.Property(propertyExpression: r => r.CreatedOnUtc).HasColumnName(name: "created_on_utc").HasColumnType(typeName: "timestamp with time zone");
        builder.Property(propertyExpression: r => r.ExpiresOnUtc).HasColumnName(name: "expires_on_utc").HasColumnType(typeName: "timestamp with time zone");

        builder.HasIndex(indexExpression: r => r.ExpiresOnUtc)
               .HasDatabaseName(name: "ix_requests_expires_on");
    }
}
