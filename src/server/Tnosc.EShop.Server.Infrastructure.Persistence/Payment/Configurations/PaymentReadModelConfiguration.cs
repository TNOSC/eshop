// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Payment.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Payment.Configurations;

/// <summary>
/// Maps <see cref="PaymentReadModel"/> onto the same <c>payment.payments</c> table the write model
/// owns. Only the write model appears in migrations; this side just reads those columns.
/// </summary>
internal sealed class PaymentReadModelConfiguration : IEntityTypeConfiguration<PaymentReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: PaymentSchema.PaymentsTable, schema: PaymentSchema.Name);
        builder.HasKey(keyExpression: payment => payment.Id);

        builder.Property(propertyExpression: payment => payment.Id).HasColumnName(name: "id");
        builder.Property(propertyExpression: payment => payment.OrderId).HasColumnName(name: "order_id");
        builder.Property(propertyExpression: payment => payment.Amount).HasColumnName(name: "amount");
        builder.Property(propertyExpression: payment => payment.Currency).HasColumnName(name: "currency");
        builder.Property(propertyExpression: payment => payment.Method).HasColumnName(name: "method");
        builder.Property(propertyExpression: payment => payment.Status).HasColumnName(name: "status");
        builder.Property(propertyExpression: payment => payment.GatewayReference).HasColumnName(name: "gateway_reference");
        builder.Property(propertyExpression: payment => payment.FailureReason).HasColumnName(name: "failure_reason");
    }
}
