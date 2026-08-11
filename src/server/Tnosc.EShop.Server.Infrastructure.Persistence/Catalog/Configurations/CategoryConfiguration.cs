// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.Lib.Infrastructure.Persistence.Extensions;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// Maps <see cref="Category"/> to <c>catalog.categories</c>.
/// </summary>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: CatalogSchema.CategoriesTable, schema: CatalogSchema.Name);
        builder.HasKey(keyExpression: category => category.Id);

        builder.Property(propertyExpression: category => category.Id)
            .HasColumnName(name: "id");

        builder.Property(propertyExpression: category => category.Name)
            .HasColumnName(name: "name")
            .HasMaxLength(maxLength: Category.NameMaxLength)
            .IsRequired();

        builder.ConfigureAggregateRootColumns();
    }
}
