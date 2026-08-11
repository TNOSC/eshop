// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.Lib.Infrastructure.Persistence.Extensions;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// Maps <see cref="Brand"/> to <c>catalog.brands</c>.
/// </summary>
internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: CatalogSchema.BrandsTable, schema: CatalogSchema.Name);
        builder.HasKey(keyExpression: brand => brand.Id);

        builder.Property(propertyExpression: brand => brand.Id)
            .HasColumnName(name: "id");

        builder.Property(propertyExpression: brand => brand.Name)
            .HasColumnName(name: "name")
            .HasMaxLength(maxLength: Brand.NameMaxLength)
            .IsRequired();

        builder.ConfigureAggregateRootColumns();
    }
}
