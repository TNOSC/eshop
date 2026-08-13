// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Configurations;

/// <summary>
/// Maps <see cref="OrderLineReadModel"/> onto the same <c>ordering.order_lines</c> table the write
/// model's owned collection produces.
/// </summary>
internal sealed class OrderLineReadModelConfiguration : IEntityTypeConfiguration<OrderLineReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderLineReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: OrderingSchema.OrderLinesTable, schema: OrderingSchema.Name);
        builder.HasKey(keyExpression: line => line.Id);

        builder.Property(propertyExpression: line => line.Id).HasColumnName(name: "id");
        builder.Property(propertyExpression: line => line.OrderId).HasColumnName(name: "order_id");
        builder.Property(propertyExpression: line => line.ProductId).HasColumnName(name: "product_id");
        builder.Property(propertyExpression: line => line.Sku).HasColumnName(name: "sku");
        builder.Property(propertyExpression: line => line.ProductName).HasColumnName(name: "product_name");
        builder.Property(propertyExpression: line => line.UnitPriceAmount).HasColumnName(name: "unit_price_amount");
        builder.Property(propertyExpression: line => line.UnitPriceCurrency).HasColumnName(name: "unit_price_currency");
        builder.Property(propertyExpression: line => line.Quantity).HasColumnName(name: "quantity");
    }
}
