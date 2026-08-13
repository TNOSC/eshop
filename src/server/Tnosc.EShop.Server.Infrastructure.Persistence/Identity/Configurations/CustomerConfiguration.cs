// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Infrastructure.Persistence.Extensions;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.Configurations;

/// <summary>
/// Maps <see cref="Customer"/> to <c>identity.customers</c>, with its addresses as an owned child
/// collection in <c>identity.customer_addresses</c>.
/// </summary>
/// <remarks>
/// The strongly-typed ids carry no <c>HasConversion</c> call: <c>EntityIdConventions</c> registers one
/// converter per id type as pre-convention model configuration. Both <c>email</c> and
/// <c>external_user_id</c> are unique-indexed — the physical backstop behind the two rules
/// <see cref="CustomerFactory"/> enforces in the domain.
/// </remarks>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <summary>
    /// The name of the unique index over the email column.
    /// </summary>
    public const string EmailIndexName = "ux_customers_email";

    /// <summary>
    /// The name of the unique index over the external user id column.
    /// </summary>
    public const string ExternalUserIdIndexName = "ux_customers_external_user_id";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.ToTable(name: IdentitySchema.CustomersTable, schema: IdentitySchema.Name);
        builder.HasKey(keyExpression: customer => customer.Id);

        builder.Property(propertyExpression: customer => customer.Id)
            .HasColumnName(name: "id");

        builder.Property(propertyExpression: customer => customer.IsActive)
            .HasColumnName(name: "is_active");

        builder.Property(propertyExpression: customer => customer.DefaultAddressId)
            .HasColumnName(name: "default_address_id");

        ConfigureValueObjects(builder: builder);
        ConfigureAddresses(builder: builder);

        builder.ConfigureAggregateRootColumns();
    }

    private static void ConfigureValueObjects(EntityTypeBuilder<Customer> builder)
    {
        builder.OwnsOne(navigationExpression: customer => customer.ExternalUserId, buildAction: externalUserId =>
        {
            externalUserId.Property(propertyExpression: value => value.Value)
                .HasColumnName(name: "external_user_id")
                .HasMaxLength(maxLength: ExternalUserId.MaxLength)
                .IsRequired();

            externalUserId.HasIndex(indexExpression: value => value.Value)
                .IsUnique()
                .HasDatabaseName(name: ExternalUserIdIndexName);
        });
        builder.Navigation(navigationExpression: customer => customer.ExternalUserId).IsRequired();

        builder.OwnsOne(navigationExpression: customer => customer.Email, buildAction: email =>
        {
            email.Property(propertyExpression: value => value.Value)
                .HasColumnName(name: "email")
                .HasMaxLength(maxLength: Email.MaxLength)
                .IsRequired();

            email.HasIndex(indexExpression: value => value.Value)
                .IsUnique()
                .HasDatabaseName(name: EmailIndexName);
        });
        builder.Navigation(navigationExpression: customer => customer.Email).IsRequired();

        builder.OwnsOne(navigationExpression: customer => customer.Name, buildAction: name =>
        {
            name.Property(propertyExpression: value => value.FirstName)
                .HasColumnName(name: "first_name")
                .HasMaxLength(maxLength: PersonName.MaxPartLength)
                .IsRequired();

            name.Property(propertyExpression: value => value.LastName)
                .HasColumnName(name: "last_name")
                .HasMaxLength(maxLength: PersonName.MaxPartLength)
                .IsRequired();
        });
        builder.Navigation(navigationExpression: customer => customer.Name).IsRequired();

        // Optional: a customer may hold no phone number at all, so this navigation stays nullable and
        // the column is not required.
        builder.OwnsOne(navigationExpression: customer => customer.PhoneNumber, buildAction: phoneNumber =>
        {
            phoneNumber.Property(propertyExpression: value => value.Value)
                .HasColumnName(name: "phone_number")
                .HasMaxLength(maxLength: PhoneNumber.MaxLength);
        });
    }

    // Addresses are an owned child collection: they have no life of their own, are only ever reached
    // through the customer, and are loaded with it — which is what lets the aggregate enforce "the
    // first address becomes the default" and "the default cannot be removed" over the whole set.
    private static void ConfigureAddresses(EntityTypeBuilder<Customer> builder)
    {
        builder.OwnsMany(navigationExpression: customer => customer.Addresses, buildAction: address =>
        {
            address.ToTable(name: IdentitySchema.AddressesTable, schema: IdentitySchema.Name);

            address.WithOwner().HasForeignKey(foreignKeyPropertyNames: "customer_id");

            address.HasKey(keyExpression: value => value.Id);

            address.Property(propertyExpression: value => value.Id)
                .HasColumnName(name: "id");

            address.Property(propertyExpression: value => value.Street)
                .HasColumnName(name: "street")
                .HasMaxLength(maxLength: Address.StreetMaxLength)
                .IsRequired();

            address.Property(propertyExpression: value => value.City)
                .HasColumnName(name: "city")
                .HasMaxLength(maxLength: Address.CityMaxLength)
                .IsRequired();

            address.Property(propertyExpression: value => value.PostalCode)
                .HasColumnName(name: "postal_code")
                .HasMaxLength(maxLength: Address.PostalCodeMaxLength)
                .IsRequired();

            address.Property(propertyExpression: value => value.Country)
                .HasColumnName(name: "country")
                .HasMaxLength(maxLength: Address.CountryLength)
                .IsFixedLength()
                .IsRequired();
        });

        // Loaded eagerly: every command on the aggregate reasons about the whole collection, so a
        // lazily-absent set would let an invariant be checked against a partial view of it.
        builder.Navigation(navigationExpression: customer => customer.Addresses)
            .AutoInclude();
    }
}
