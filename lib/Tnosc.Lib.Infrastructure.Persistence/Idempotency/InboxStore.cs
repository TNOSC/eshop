// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.Lib.Application.Abstractions.Persistence;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Postgres-backed <see cref="IInboxStore"/> writing through <typeparamref name="TContext"/>, so the
/// claim commits with the handler's own writes and a crash leaves neither behind.
/// </summary>
/// <typeparam name="TContext">The write <see cref="DbContext"/> that owns the inbox table.</typeparam>
/// <param name="context">The write context whose connection and transaction the claim joins.</param>
/// <param name="timeProvider">Supplies the current UTC time for claim stamping.</param>
internal sealed class InboxStore<TContext>(
    TContext context,
    TimeProvider timeProvider)
    : IInboxStore
    where TContext : DbContext
{
    /// <inheritdoc />
    public async ValueTask<bool> TryClaimAsync(
        Guid eventId,
        string handlerName,
        CancellationToken cancellationToken = default)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        int inserted = await context.Database.ExecuteSqlRawAsync(
            sql: IdempotencySql.ClaimEvent,
            parameters: [eventId, handlerName, now],
            cancellationToken: cancellationToken);

        return inserted == 1;
    }
}
