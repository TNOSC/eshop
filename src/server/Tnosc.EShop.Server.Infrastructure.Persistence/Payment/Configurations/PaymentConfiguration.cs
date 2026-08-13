// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Infrastructure.Persistence.Extensions;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Payment.Configurations;

/// <summary>
/// Maps <see cref="PaymentAggregate"/> to <c>payment.payments</c>.
/// </summary>
/// <remarks>
/// <see cref="PaymentAggregate.OrderId"/> is a plain <see cref="Guid"/> column with no foreign key to
/// <c>ordering.orders</c> — the same deliberate choice <c>OrderConfiguration</c> makes for its own
/// lines against <c>catalog.products</c>. Payment must survive independently of Ordering's own
/// lifecycle, and a cross-schema constraint would couple the two contexts at the database level, not
/// just in code.
/// </remarks>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentAggregate>
{
    /// <summary>
    /// The name of the unique index over the order identifier column — the physical backstop behind
    /// <c>PaymentFactory</c>'s "one payment per order" invariant.
    /// </summary>
    public const string OrderIdIndexName = "ux_payments_order_id";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentAggregate> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: PaymentSchema.PaymentsTable, schema: PaymentSchema.Name);
        builder.HasKey(keyExpression: payment => payment.Id);

        builder.Property(propertyExpression: payment => payment.Id)
            .HasColumnName(name: "id");

        builder.Property(propertyExpression: payment => payment.OrderId)
            .HasColumnName(name: "order_id")
            .IsRequired();

        builder.Property(propertyExpression: payment => payment.Method)
            .HasColumnName(name: "method")
            .HasConversion<string>()
            .HasMaxLength(maxLength: 20)
            .IsRequired();

        // Stored as its name rather than its ordinal, matching OrderConfiguration's Status column —
        // readable by an operator during an incident, and immune to the enum being renumbered later.
        builder.Property(propertyExpression: payment => payment.Status)
            .HasColumnName(name: "status")
            .HasConversion<string>()
            .HasMaxLength(maxLength: 20)
            .IsRequired();

        builder.Property(propertyExpression: payment => payment.GatewayReference)
            .HasColumnName(name: "gateway_reference")
            .HasMaxLength(maxLength: 200);

        builder.Property(propertyExpression: payment => payment.FailureReason)
            .HasColumnName(name: "failure_reason")
            .HasMaxLength(maxLength: 500);

        builder.HasIndex(indexExpression: payment => payment.OrderId)
            .IsUnique()
            .HasDatabaseName(name: OrderIdIndexName);

        builder.OwnsOne(navigationExpression: payment => payment.Amount, buildAction: amount =>
        {
            amount.Property(propertyExpression: money => money.Amount)
                .HasColumnName(name: "amount")
                .HasPrecision(precision: 18, scale: 2)
                .IsRequired();

            amount.Property(propertyExpression: money => money.Currency)
                .HasColumnName(name: "currency")
                .HasMaxLength(maxLength: Money.CurrencyLength)
                .IsFixedLength()
                .IsRequired();
        });
        builder.Navigation(navigationExpression: payment => payment.Amount).IsRequired();

        builder.ConfigureAggregateRootColumns();
    }
}
