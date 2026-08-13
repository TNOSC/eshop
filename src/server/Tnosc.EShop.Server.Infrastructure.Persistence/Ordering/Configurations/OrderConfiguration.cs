// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Infrastructure.Persistence.Extensions;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Configurations;

/// <summary>
/// Maps <see cref="Order"/> to <c>ordering.orders</c>, with its lines as an owned child collection in
/// <c>ordering.order_lines</c>.
/// </summary>
/// <remarks>
/// The strongly-typed ids carry no <c>HasConversion</c> call — <c>EntityIdConventions</c> registers one
/// converter per id type as pre-convention model configuration. There is no foreign key to
/// <c>catalog.products</c> or <c>identity.customers</c>, and that is deliberate rather than an
/// oversight: an order line snapshots a product by value and must survive that product being
/// discontinued or removed, so a constraint pointing at another context's table would turn a
/// catalogue clean-up into a failure to write order history.
/// </remarks>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>
    /// The name of the unique index over the order number column.
    /// </summary>
    public const string OrderNumberIndexName = "ux_orders_order_number";

    /// <summary>
    /// The name of the index over the customer column.
    /// </summary>
    public const string CustomerIndexName = "ix_orders_customer_id";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: OrderingSchema.OrdersTable, schema: OrderingSchema.Name);
        builder.HasKey(keyExpression: order => order.Id);

        builder.Property(propertyExpression: order => order.Id)
            .HasColumnName(name: "id");

        builder.Property(propertyExpression: order => order.CustomerId)
            .HasColumnName(name: "customer_id")
            .IsRequired();

        // Stored as its name rather than its ordinal: a status column read by an operator during an
        // incident is worth more as 'Shipped' than as '3', and renumbering the enum can then never
        // silently reinterpret existing rows.
        builder.Property(propertyExpression: order => order.Status)
            .HasColumnName(name: "status")
            .HasConversion<string>()
            .HasMaxLength(maxLength: 20)
            .IsRequired();

        builder.Property(propertyExpression: order => order.PlacedOnUtc)
            .HasColumnName(name: "placed_on_utc")
            .IsRequired();

        // Computed from the lines, so it has no column and no backing field for EF to find.
        builder.Ignore(propertyExpression: order => order.Subtotal);

        builder.HasIndex(indexExpression: order => order.CustomerId)
            .HasDatabaseName(name: CustomerIndexName);

        ConfigureValueObjects(builder: builder);
        ConfigureLines(builder: builder);

        builder.ConfigureAggregateRootColumns();
    }

    private static void ConfigureValueObjects(EntityTypeBuilder<Order> builder)
    {
        builder.OwnsOne(navigationExpression: order => order.Number, buildAction: number =>
        {
            number.Property(propertyExpression: value => value.Value)
                .HasColumnName(name: "order_number")
                .HasMaxLength(maxLength: OrderNumber.Length)
                .IsFixedLength()
                .IsRequired();

            // The physical backstop behind OrderNumber.Generate's random suffix: a collision fails the
            // insert rather than handing two orders the same customer-facing reference.
            number.HasIndex(indexExpression: value => value.Value)
                .IsUnique()
                .HasDatabaseName(name: OrderNumberIndexName);
        });
        builder.Navigation(navigationExpression: order => order.Number).IsRequired();

        builder.OwnsOne(navigationExpression: order => order.Total, buildAction: total =>
        {
            total.Property(propertyExpression: money => money.Amount)
                .HasColumnName(name: "total_amount")
                .HasPrecision(precision: 18, scale: 2)
                .IsRequired();

            total.Property(propertyExpression: money => money.Currency)
                .HasColumnName(name: "total_currency")
                .HasMaxLength(maxLength: Money.CurrencyLength)
                .IsFixedLength()
                .IsRequired();
        });
        builder.Navigation(navigationExpression: order => order.Total).IsRequired();

        builder.OwnsOne(navigationExpression: order => order.ShippingAddress, buildAction: address =>
        {
            address.Property(propertyExpression: value => value.Street)
                .HasColumnName(name: "shipping_street")
                .HasMaxLength(maxLength: ShippingAddress.StreetMaxLength)
                .IsRequired();

            address.Property(propertyExpression: value => value.City)
                .HasColumnName(name: "shipping_city")
                .HasMaxLength(maxLength: ShippingAddress.CityMaxLength)
                .IsRequired();

            address.Property(propertyExpression: value => value.PostalCode)
                .HasColumnName(name: "shipping_postal_code")
                .HasMaxLength(maxLength: ShippingAddress.PostalCodeMaxLength)
                .IsRequired();

            address.Property(propertyExpression: value => value.Country)
                .HasColumnName(name: "shipping_country")
                .HasMaxLength(maxLength: ShippingAddress.CountryLength)
                .IsFixedLength()
                .IsRequired();
        });
        builder.Navigation(navigationExpression: order => order.ShippingAddress).IsRequired();
    }

    // Lines are an owned child collection: they have no life outside their order, are only ever
    // reached through it, and are loaded with it — which is what lets Order.Subtotal be computed over
    // the whole set rather than over whatever happened to be loaded.
    private static void ConfigureLines(EntityTypeBuilder<Order> builder)
    {
        builder.OwnsMany(navigationExpression: order => order.Lines, buildAction: line =>
        {
            line.ToTable(name: OrderingSchema.OrderLinesTable, schema: OrderingSchema.Name);

            line.WithOwner().HasForeignKey(foreignKeyPropertyNames: "order_id");

            line.HasKey(keyExpression: value => value.Id);

            line.Property(propertyExpression: value => value.Id)
                .HasColumnName(name: "id");

            line.Property(propertyExpression: value => value.ProductId)
                .HasColumnName(name: "product_id")
                .IsRequired();

            line.Property(propertyExpression: value => value.Sku)
                .HasColumnName(name: "sku")
                .HasMaxLength(maxLength: OrderLine.SkuMaxLength)
                .IsRequired();

            line.Property(propertyExpression: value => value.ProductName)
                .HasColumnName(name: "product_name")
                .HasMaxLength(maxLength: OrderLine.ProductNameMaxLength)
                .IsRequired();

            line.Ignore(propertyExpression: value => value.LineTotal);

            line.OwnsOne(navigationExpression: value => value.UnitPrice, buildAction: unitPrice =>
            {
                unitPrice.Property(propertyExpression: money => money.Amount)
                    .HasColumnName(name: "unit_price_amount")
                    .HasPrecision(precision: 18, scale: 2)
                    .IsRequired();

                unitPrice.Property(propertyExpression: money => money.Currency)
                    .HasColumnName(name: "unit_price_currency")
                    .HasMaxLength(maxLength: Money.CurrencyLength)
                    .IsFixedLength()
                    .IsRequired();
            });
            line.Navigation(navigationExpression: value => value.UnitPrice).IsRequired();

            line.OwnsOne(navigationExpression: value => value.Quantity, buildAction: quantity =>
            {
                quantity.Property(propertyExpression: value => value.Value)
                    .HasColumnName(name: "quantity")
                    .IsRequired();
            });
            line.Navigation(navigationExpression: value => value.Quantity).IsRequired();
        });

        builder.Navigation(navigationExpression: order => order.Lines)
            .AutoInclude();
    }
}
