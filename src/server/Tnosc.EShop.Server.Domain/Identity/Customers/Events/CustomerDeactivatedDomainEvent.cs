// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Identity.Customers.Events;

/// <summary>
/// Raised when a customer's profile is deactivated.
/// </summary>
/// <remarks>
/// Local only: this does not disable the Keycloak account, so a handler reacting to this event still
/// cannot assume the identity provider has revoked access. See <see cref="Customer.Deactivate"/>.
/// </remarks>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="CustomerId">The identifier of the customer that was deactivated.</param>
[DomainEventName("identity.customer-deactivated.v1")]
public sealed record CustomerDeactivatedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid CustomerId) : IDomainEvent;
