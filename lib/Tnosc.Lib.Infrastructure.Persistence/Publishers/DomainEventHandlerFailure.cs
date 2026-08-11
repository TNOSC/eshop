// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Infrastructure.Persistence.Publishers;

/// <summary>
/// One handler's failure to process one domain event.
/// </summary>
/// <remarks>
/// Reported rather than thrown, because a failure here is not the end of delivery: every other
/// handler for the same event still runs. The caller decides what an accumulated set of failures
/// means — currently, that the outbox message is retried and eventually dead-lettered per handler.
/// </remarks>
/// <param name="EventId">The <see cref="Tnosc.Lib.Domain.IDomainEvent.Id"/> of the event being delivered.</param>
/// <param name="HandlerName">The durable name of the handler that failed, from <see cref="Tnosc.Lib.Application.Decorators.HandlerChain"/>.</param>
/// <param name="Exception">The exception the handler threw.</param>
public sealed record DomainEventHandlerFailure(
    Guid EventId,
    string HandlerName,
    Exception Exception);
