// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A test-only domain event whose registered handler, <see cref="PoisonTestDomainEventHandler"/>,
/// always throws. Used to exercise the outbox processor's failure, backoff, and dead-lettering
/// behaviour, and to prove one poison message does not block the rest of a batch.
/// </summary>
/// <param name="Id">The domain event identifier.</param>
/// <param name="OccurredOnUtc">The UTC date and time the event occurred.</param>
/// <param name="AggregateId">The identifier of the aggregate that raised the event.</param>
[DomainEventName("test.poison-event.v1")]
internal sealed record PoisonTestDomainEvent(Guid Id, DateTime OccurredOnUtc, Guid AggregateId) : IDomainEvent;
