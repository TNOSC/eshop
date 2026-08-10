// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// Maps <see cref="CategoryReadModel"/> onto <c>catalog.categories</c>.
/// </summary>
internal sealed class CategoryReadModelConfiguration : IEntityTypeConfiguration<CategoryReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CategoryReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: CatalogSchema.CategoriesTable, schema: CatalogSchema.Name);
        builder.HasKey(keyExpression: category => category.Id);

        builder.Property(propertyExpression: category => category.Id).HasColumnName(name: "id");
        builder.Property(propertyExpression: category => category.Name).HasColumnName(name: "name");
    }
}
