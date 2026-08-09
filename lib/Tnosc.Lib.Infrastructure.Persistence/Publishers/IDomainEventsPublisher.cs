// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain;

namespace Tnosc.Lib.Infrastructure.Publishers;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IDomainEventsPublisher
{
    /// <summary>
    /// Publishes a domain event to all registered handlers
    /// </summary>
    /// <param name="domainEvents">The domain events instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A value task representing the asynchronous operation</returns>
    ValueTask PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
