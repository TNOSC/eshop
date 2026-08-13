// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Infrastructure.Persistence.Extensions;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// Maps <see cref="Product"/> to <c>catalog.products</c>.
/// </summary>
/// <remarks>
/// The strongly-typed ids carry no <c>HasConversion</c> call: <c>EntityIdConventions</c> registers one
/// converter per id type as pre-convention model configuration, which also covers the foreign keys.
/// The three value objects map as owned types onto this same table, and <c>sku</c> is unique-indexed —
/// the physical backstop behind the uniqueness rule <c>ProductFactory</c> enforces in the domain.
/// </remarks>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>
    /// The name of the unique index over the SKU column.
    /// </summary>
    public const string SkuIndexName = "ux_products_sku";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: CatalogSchema.ProductsTable, schema: CatalogSchema.Name);
        builder.HasKey(keyExpression: product => product.Id);

        builder.Property(propertyExpression: product => product.Id)
            .HasColumnName(name: "id");

        builder.Property(propertyExpression: product => product.Name)
            .HasColumnName(name: "name")
            .HasMaxLength(maxLength: Product.NameMaxLength)
            .IsRequired();

        builder.Property(propertyExpression: product => product.Description)
            .HasColumnName(name: "description")
            .HasMaxLength(maxLength: Product.DescriptionMaxLength);

        builder.Property(propertyExpression: product => product.BrandId)
            .HasColumnName(name: "brand_id")
            .IsRequired();

        builder.Property(propertyExpression: product => product.CategoryId)
            .HasColumnName(name: "category_id")
            .IsRequired();

        builder.Property(propertyExpression: product => product.IsDiscontinued)
            .HasColumnName(name: "is_discontinued");

        ConfigureValueObjects(builder: builder);
        ConfigureRelationships(builder: builder);

        builder.ConfigureAggregateRootColumns();
    }

    private static void ConfigureValueObjects(EntityTypeBuilder<Product> builder)
    {
        builder.OwnsOne(navigationExpression: product => product.Sku, buildAction: sku =>
        {
            sku.Property(propertyExpression: value => value.Value)
                .HasColumnName(name: "sku")
                .HasMaxLength(maxLength: Sku.MaxLength)
                .IsRequired();

            sku.HasIndex(indexExpression: value => value.Value)
                .IsUnique()
                .HasDatabaseName(name: SkuIndexName);
        });
        builder.Navigation(navigationExpression: product => product.Sku).IsRequired();

        builder.OwnsOne(navigationExpression: product => product.Price, buildAction: price =>
        {
            price.Property(propertyExpression: money => money.Amount)
                .HasColumnName(name: "price_amount")
                .HasPrecision(precision: 18, scale: 2)
                .IsRequired();

            price.Property(propertyExpression: money => money.Currency)
                .HasColumnName(name: "price_currency")
                .HasMaxLength(maxLength: Money.CurrencyLength)
                .IsFixedLength()
                .IsRequired();
        });
        builder.Navigation(navigationExpression: product => product.Price).IsRequired();

        builder.OwnsOne(navigationExpression: product => product.Stock, buildAction: stock =>
        {
            stock.Property(propertyExpression: quantity => quantity.Value)
                .HasColumnName(name: "stock_quantity")
                .IsRequired();
        });
        builder.Navigation(navigationExpression: product => product.Stock).IsRequired();
    }

    // No navigation properties: aggregates reference each other by identifier only. The constraints
    // still belong in the database — the search query joins across all three tables.
    private static void ConfigureRelationships(EntityTypeBuilder<Product> builder)
    {
        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression: product => product.BrandId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression: product => product.CategoryId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict);
    }
}
