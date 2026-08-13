// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Domain.Identity.Customers.Events;

/// <summary>
/// Raised when a customer's local email copy is reconciled to a new value from the identity provider.
/// </summary>
/// <remarks>
/// Raised by <see cref="Customer.SyncEmail"/> only when the value actually changed. A repeat login
/// presenting the same address is a no-op, so this event does not fire once per request.
/// </remarks>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="CustomerId">The identifier of the customer whose email changed.</param>
/// <param name="OldEmail">The email address held before reconciliation.</param>
/// <param name="NewEmail">The email address taken from the identity provider.</param>
[DomainEventName("identity.customer-email-changed.v1")]
public sealed record CustomerEmailChangedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid CustomerId,
    string OldEmail,
    string NewEmail) : IDomainEvent;
