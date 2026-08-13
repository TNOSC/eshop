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
/// Maps <see cref="CustomerReadModel"/> onto the same <c>identity.customers</c> table the write model
/// owns. Only the write model appears in migrations; this side just reads those columns.
/// </summary>
internal sealed class CustomerReadModelConfiguration : IEntityTypeConfiguration<CustomerReadModel>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CustomerReadModel> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: IdentitySchema.CustomersTable, schema: IdentitySchema.Name);
        builder.HasKey(keyExpression: customer => customer.Id);

        builder.Property(propertyExpression: customer => customer.Id).HasColumnName(name: "id");
        builder.Property(propertyExpression: customer => customer.ExternalUserId).HasColumnName(name: "external_user_id");
        builder.Property(propertyExpression: customer => customer.Email).HasColumnName(name: "email");
        builder.Property(propertyExpression: customer => customer.FirstName).HasColumnName(name: "first_name");
        builder.Property(propertyExpression: customer => customer.LastName).HasColumnName(name: "last_name");
        builder.Property(propertyExpression: customer => customer.PhoneNumber).HasColumnName(name: "phone_number");
        builder.Property(propertyExpression: customer => customer.IsActive).HasColumnName(name: "is_active");
        builder.Property(propertyExpression: customer => customer.DefaultAddressId).HasColumnName(name: "default_address_id");

        builder.HasMany(navigationExpression: customer => customer.Addresses)
            .WithOne()
            .HasForeignKey(foreignKeyExpression: address => address.CustomerId);
    }
}
