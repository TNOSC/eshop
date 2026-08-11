// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A test-only domain event carrying several properties beyond <see cref="IDomainEvent.Id"/> and
/// <see cref="IDomainEvent.OccurredOnUtc"/>, used by the B1 regression test to prove that an outbox
/// row's <c>Content</c> survives round-tripping the event's full payload, not just its base shape.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="AggregateId">The identifier of the aggregate that raised the event.</param>
/// <param name="Name">The aggregate's name at the time of creation.</param>
/// <param name="Note">An arbitrary free-text note — one of the "extra" properties B1 used to drop.</param>
/// <param name="Amount">An arbitrary numeric value — another of the "extra" properties B1 used to drop.</param>
/// <param name="Tags">An arbitrary collection — proves nested/complex extra properties survive too.</param>
[DomainEventName("test.aggregate-created.v1")]
internal sealed record TestAggregateCreatedDomainEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid AggregateId,
    string Name,
    string Note,
    int Amount,
    IReadOnlyCollection<string> Tags) : IDomainEvent;
