// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A test-only domain event whose registered handler, <see cref="FlakyTestDomainEventHandler"/>,
/// fails a configured number of times before succeeding. Used to prove that <c>[Retry]</c> absorbs a
/// transient failure in-process, so the outbox never sees it.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="AggregateId">The identifier of the aggregate that raised the event.</param>
[DomainEventName("test.flaky-event.v1")]
internal sealed record FlakyTestDomainEvent(Guid Id, DateTime OccurredOnUtc, Guid AggregateId) : IDomainEvent;
