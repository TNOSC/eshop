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
/// Maps <see cref="OrderReadModel"/> onto the same <c>ordering.orders</c> table the write model owns.
/// Only the write model appears in migrations; this side just reads those columns.
/// </summary>
internal sealed class OrderReadModelConfiguration : IEntityTypeConfiguration<OrderReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: OrderingSchema.OrdersTable, schema: OrderingSchema.Name);
        builder.HasKey(keyExpression: order => order.Id);

        builder.Property(propertyExpression: order => order.Id).HasColumnName(name: "id");
        builder.Property(propertyExpression: order => order.OrderNumber).HasColumnName(name: "order_number");
        builder.Property(propertyExpression: order => order.CustomerId).HasColumnName(name: "customer_id");
        builder.Property(propertyExpression: order => order.Status).HasColumnName(name: "status");
        builder.Property(propertyExpression: order => order.TotalAmount).HasColumnName(name: "total_amount");
        builder.Property(propertyExpression: order => order.TotalCurrency).HasColumnName(name: "total_currency");
        builder.Property(propertyExpression: order => order.PlacedOnUtc).HasColumnName(name: "placed_on_utc");
        builder.Property(propertyExpression: order => order.ShippingStreet).HasColumnName(name: "shipping_street");
        builder.Property(propertyExpression: order => order.ShippingCity).HasColumnName(name: "shipping_city");
        builder.Property(propertyExpression: order => order.ShippingPostalCode).HasColumnName(name: "shipping_postal_code");
        builder.Property(propertyExpression: order => order.ShippingCountry).HasColumnName(name: "shipping_country");

        builder.HasMany(navigationExpression: order => order.Lines)
            .WithOne()
            .HasForeignKey(foreignKeyExpression: line => line.OrderId);
    }
}
