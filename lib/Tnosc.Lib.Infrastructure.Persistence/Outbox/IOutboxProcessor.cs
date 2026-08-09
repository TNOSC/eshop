// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.Lib.Infrastructure.Persistence.Outbox;

/// <summary>
/// Claims and processes a single batch of pending outbox messages.
/// </summary>
/// <remarks>
/// A plain injectable service rather than a hosted service, so it can be driven directly — by
/// <see cref="OutboxBackgroundService{TContext}"/> in-process, or by an integration test against a
/// real database with no host required.
/// </remarks>
public interface IOutboxProcessor
{
    /// <summary>
    /// Claims and processes one batch of pending outbox messages.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The number of messages claimed and attempted in this batch.</returns>
    ValueTask<int> ProcessBatchAsync(CancellationToken cancellationToken);
}
