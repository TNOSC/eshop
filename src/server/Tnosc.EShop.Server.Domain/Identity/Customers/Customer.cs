// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Tnosc.EShop.Server.Domain.Identity.Customers.Events;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// The shopper profile that hangs off an identity-provider account. Owns every rule about its own
/// name, phone number and addresses.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The profile boundary is the thing to hold on to.</strong> Name, phone number and addresses
/// are this codebase's; <em>email and password are Keycloak's</em>. There is deliberately no
/// <c>ChangeEmail</c> operation and no way to set a password here — the customer does both in
/// Keycloak's Account Console, and <see cref="SyncEmail"/> pulls the resulting email across on the
/// next login.
/// </para>
/// <para>
/// A customer is never created directly: <see cref="Register"/> is <see langword="internal"/> behind
/// <see cref="CustomerFactory"/>, which owns the email-uniqueness invariant that a single instance
/// cannot see.
/// </para>
/// </remarks>
public sealed class Customer : AggregateRoot<CustomerId>
{
    private readonly List<Address> _addresses = [];

    private Customer()
    {
        // EF.
    }

    /// <summary>
    /// Gets the identity provider's subject identifier for this customer.
    /// </summary>
    public ExternalUserId ExternalUserId { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's email address, reconciled from the identity provider.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's name.
    /// </summary>
    public PersonName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the customer's optional contact number.
    /// </summary>
    public PhoneNumber? PhoneNumber { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the customer is still active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the identifier of the customer's default address, or <see langword="null"/> while they
    /// hold none.
    /// </summary>
    /// <remarks>
    /// Kept here rather than as an <c>IsDefault</c> flag on <see cref="Address"/>, so "exactly one
    /// default" is a single field this aggregate maintains instead of an invariant spread across rows.
    /// </remarks>
    public AddressId? DefaultAddressId { get; private set; }

    /// <summary>
    /// Gets the customer's addresses.
    /// </summary>
    /// <remarks>
    /// Exposed read-only over a private backing list, so every mutation goes through a method on this
    /// aggregate and the collection's invariants cannot be sidestepped.
    /// </remarks>
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    /// <summary>
    /// Registers a customer profile for an identity-provider account and raises
    /// <see cref="CustomerRegisteredDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately <see langword="internal"/>: email uniqueness spans every customer, so it cannot be
    /// decided here. <see cref="CustomerFactory.ProvisionAsync"/> is the only way in from outside the
    /// domain, and it consults <see cref="ICustomerRepository"/> first — which is what keeps the
    /// uniqueness rule out of the command handler.
    /// </remarks>
    /// <param name="externalUserId">The identity provider's subject identifier.</param>
    /// <param name="email">The email address taken from the token.</param>
    /// <param name="name">The customer's name.</param>
    /// <param name="phoneNumber">The customer's optional contact number.</param>
    /// <returns>The registered customer.</returns>
    internal static Customer Register(
        ExternalUserId externalUserId,
        Email email,
        PersonName name,
        PhoneNumber? phoneNumber)
    {
        var customer = new Customer
        {
            Id = CustomerId.New(),
            ExternalUserId = externalUserId,
            Email = email,
            Name = name,
            PhoneNumber = phoneNumber,
            IsActive = true,
        };

        customer.IncrementVersion();
        customer.AddDomainEvent(domainEvent: new CustomerRegisteredDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            CustomerId: customer.Id.Value,
            ExternalUserId: customer.ExternalUserId.Value,
            Email: customer.Email.Value,
            FirstName: customer.Name.FirstName,
            LastName: customer.Name.LastName));

        return customer;
    }

    /// <summary>
    /// Reconciles the local email copy with the identity provider's current value.
    /// </summary>
    /// <remarks>
    /// Reconciliation, not an edit. The client calls this path on every login, so it must be a no-op
    /// when nothing changed: an unconditional assignment would increment the version and raise an
    /// event — and therefore write an outbox row — once per request.
    /// </remarks>
    /// <param name="email">The email address taken from the current token.</param>
    /// <returns>
    /// Success — including when the address is unchanged — or a <c>Customer.Deactivated</c> conflict.
    /// </returns>
    internal Result SyncEmail(Email email)
    {
        if (!IsActive)
        {
            return CustomerErrors.Deactivated(customerId: Id.Value);
        }

        if (Email == email)
        {
            return Result.Success();
        }

        Email oldEmail = Email;
        Email = email;
        IncrementVersion();

        AddDomainEvent(domainEvent: new CustomerEmailChangedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            CustomerId: Id.Value,
            OldEmail: oldEmail.Value,
            NewEmail: email.Value));

        return Result.Success();
    }

    /// <summary>
    /// Updates the parts of the profile this codebase owns — name and phone number.
    /// </summary>
    /// <param name="name">The customer's new name.</param>
    /// <param name="phoneNumber">The customer's new contact number, or <see langword="null"/> to clear it.</param>
    /// <returns>Success, or a <c>Customer.Deactivated</c> conflict.</returns>
    public Result UpdateProfile(PersonName name, PhoneNumber? phoneNumber)
    {
        if (!IsActive)
        {
            return CustomerErrors.Deactivated(customerId: Id.Value);
        }

        Name = name;
        PhoneNumber = phoneNumber;
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Adds an address, making it the default when it is the customer's first.
    /// </summary>
    /// <param name="street">The street line.</param>
    /// <param name="city">The city.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="country">The ISO 3166-1 alpha-2 country code.</param>
    /// <returns>
    /// The identifier of the new address, a <c>Customer.Deactivated</c> conflict, or one of the
    /// <c>Address.*</c> validation errors.
    /// </returns>
    public Result<AddressId> AddAddress(
        string? street,
        string? city,
        string? postalCode,
        string? country)
    {
        if (!IsActive)
        {
            return CustomerErrors.Deactivated(customerId: Id.Value);
        }

        Result<Address> address = Address.Create(
            street: street,
            city: city,
            postalCode: postalCode,
            country: country);

        if (address.IsError)
        {
            return address.Errors.ToArray();
        }

        _addresses.Add(item: address.Value);

        // The first address a customer ever adds becomes their default, so a customer who holds any
        // address always has one selected and no caller has to remember to set it.
        DefaultAddressId ??= address.Value.Id;

        IncrementVersion();

        return address.Value.Id;
    }

    /// <summary>
    /// Replaces the contents of one of the customer's addresses.
    /// </summary>
    /// <param name="addressId">The identifier of the address to update.</param>
    /// <param name="street">The new street line.</param>
    /// <param name="city">The new city.</param>
    /// <param name="postalCode">The new postal code.</param>
    /// <param name="country">The new ISO 3166-1 alpha-2 country code.</param>
    /// <returns>
    /// Success, a <c>Customer.Deactivated</c> conflict, <c>Address.NotFound</c>, or one of the
    /// <c>Address.*</c> validation errors.
    /// </returns>
    public Result UpdateAddress(
        AddressId addressId,
        string? street,
        string? city,
        string? postalCode,
        string? country)
    {
        if (!IsActive)
        {
            return CustomerErrors.Deactivated(customerId: Id.Value);
        }

        Address? address = Find(addressId: addressId);

        if (address is null)
        {
            return AddressErrors.NotFound(addressId: addressId.Value);
        }

        Result updated = address.Update(
            street: street,
            city: city,
            postalCode: postalCode,
            country: country);

        if (updated.IsError)
        {
            return updated.Errors.ToArray();
        }

        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Removes one of the customer's addresses.
    /// </summary>
    /// <remarks>
    /// The default address cannot be removed. Requiring another address to be made the default first
    /// keeps "a customer with addresses has a default" true at every point, instead of silently
    /// promoting an arbitrary replacement on the customer's behalf.
    /// </remarks>
    /// <param name="addressId">The identifier of the address to remove.</param>
    /// <returns>
    /// Success, a <c>Customer.Deactivated</c> conflict, <c>Address.NotFound</c>, or
    /// <c>Address.CannotRemoveDefault</c>.
    /// </returns>
    public Result RemoveAddress(AddressId addressId)
    {
        if (!IsActive)
        {
            return CustomerErrors.Deactivated(customerId: Id.Value);
        }

        Address? address = Find(addressId: addressId);

        if (address is null)
        {
            return AddressErrors.NotFound(addressId: addressId.Value);
        }

        if (DefaultAddressId == addressId)
        {
            return AddressErrors.CannotRemoveDefault(addressId: addressId.Value);
        }

        _addresses.Remove(item: address);
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Makes one of the customer's addresses their default.
    /// </summary>
    /// <param name="addressId">The identifier of the address to make default.</param>
    /// <returns>Success, a <c>Customer.Deactivated</c> conflict, or <c>Address.NotFound</c>.</returns>
    public Result SetDefaultAddress(AddressId addressId)
    {
        if (!IsActive)
        {
            return CustomerErrors.Deactivated(customerId: Id.Value);
        }

        Address? address = Find(addressId: addressId);

        if (address is null)
        {
            return AddressErrors.NotFound(addressId: addressId.Value);
        }

        DefaultAddressId = address.Id;
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the customer, closing the profile to any further change.
    /// </summary>
    /// <remarks>
    /// Local only: this does not disable the Keycloak account, which an operator does in the admin
    /// console. Nothing in this codebase writes to the identity provider.
    /// </remarks>
    /// <returns>Success, or a <c>Customer.AlreadyDeactivated</c> conflict.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
        {
            return CustomerErrors.AlreadyDeactivated(customerId: Id.Value);
        }

        IsActive = false;
        IncrementVersion();

        AddDomainEvent(domainEvent: new CustomerDeactivatedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            CustomerId: Id.Value));

        return Result.Success();
    }

    private Address? Find(AddressId addressId) =>
        _addresses.FirstOrDefault(predicate: address => address.Id == addressId);
}
