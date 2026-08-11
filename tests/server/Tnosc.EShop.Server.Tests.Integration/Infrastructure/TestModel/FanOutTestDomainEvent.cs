// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A test-only domain event with <b>two</b> registered handlers — <see cref="FanOutFailingHandler"/>
/// and <see cref="FanOutSucceedingHandler"/> — used to prove that one handler's failure neither
/// blocks its sibling nor dead-letters it.
/// </summary>
/// <remarks>
/// Deliberately its own event type rather than a second handler on
/// <see cref="TestAggregateCreatedDomainEvent"/>: adding a handler there would change the delivery
/// counts every existing outbox test asserts on.
/// </remarks>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="AggregateId">The identifier of the aggregate that raised the event.</param>
[DomainEventName("test.fan-out-event.v1")]
internal sealed record FanOutTestDomainEvent(Guid Id, DateTime OccurredOnUtc, Guid AggregateId) : IDomainEvent;
