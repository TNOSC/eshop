// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Identity.Customers.Events;

/// <summary>
/// Raised the first time a customer profile is provisioned for an identity-provider account.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="CustomerId">The identifier of the registered customer.</param>
/// <param name="ExternalUserId">The identity provider's subject identifier for the customer.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="FirstName">The customer's given name.</param>
/// <param name="LastName">The customer's family name.</param>
[DomainEventName("identity.customer-registered.v1")]
public sealed record CustomerRegisteredDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid CustomerId,
    string ExternalUserId,
    string Email,
    string FirstName,
    string LastName) : IDomainEvent;
