// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// Maps <see cref="ProductReadModel"/> onto the same <c>catalog.products</c> table the write model
/// owns. Only the write model appears in migrations; this side just reads those columns.
/// </summary>
internal sealed class ProductReadModelConfiguration : IEntityTypeConfiguration<ProductReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: CatalogSchema.ProductsTable, schema: CatalogSchema.Name);
        builder.HasKey(keyExpression: product => product.Id);

        builder.Property(propertyExpression: product => product.Id).HasColumnName(name: "id");
        builder.Property(propertyExpression: product => product.Sku).HasColumnName(name: "sku");
        builder.Property(propertyExpression: product => product.Name).HasColumnName(name: "name");
        builder.Property(propertyExpression: product => product.Description).HasColumnName(name: "description");
        builder.Property(propertyExpression: product => product.PriceAmount).HasColumnName(name: "price_amount");
        builder.Property(propertyExpression: product => product.PriceCurrency).HasColumnName(name: "price_currency");
        builder.Property(propertyExpression: product => product.StockQuantity).HasColumnName(name: "stock_quantity");
        builder.Property(propertyExpression: product => product.BrandId).HasColumnName(name: "brand_id");
        builder.Property(propertyExpression: product => product.CategoryId).HasColumnName(name: "category_id");
        builder.Property(propertyExpression: product => product.IsDiscontinued).HasColumnName(name: "is_discontinued");
        builder.Property(propertyExpression: product => product.ImageUrl).HasColumnName(name: "image_url");
    }
}
