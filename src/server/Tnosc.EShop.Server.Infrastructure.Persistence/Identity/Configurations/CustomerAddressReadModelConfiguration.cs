// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Identity.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.Configurations;

/// <summary>
/// Maps <see cref="CustomerAddressReadModel"/> onto the same <c>identity.customer_addresses</c> table
/// the write model's owned collection produces.
/// </summary>
internal sealed class CustomerAddressReadModelConfiguration : IEntityTypeConfiguration<CustomerAddressReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CustomerAddressReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: IdentitySchema.AddressesTable, schema: IdentitySchema.Name);
        builder.HasKey(keyExpression: address => address.Id);

        builder.Property(propertyExpression: address => address.Id).HasColumnName(name: "id");
        builder.Property(propertyExpression: address => address.CustomerId).HasColumnName(name: "customer_id");
        builder.Property(propertyExpression: address => address.Street).HasColumnName(name: "street");
        builder.Property(propertyExpression: address => address.City).HasColumnName(name: "city");
        builder.Property(propertyExpression: address => address.PostalCode).HasColumnName(name: "postal_code");
        builder.Property(propertyExpression: address => address.Country).HasColumnName(name: "country");
    }
}
