// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>The typed client for the Ordering bounded context's API.</summary>
public interface IOrderingApi
{
    /// <summary>Lists the caller's own orders, newest first.</summary>
    /// <param name="page">The requested page.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<PagedResult<OrderSummary>>> GetMyOrdersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Reads one of the caller's own orders, with its lines.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Order>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Turns the caller's basket into an order.</summary>
    /// <param name="idempotencyKey">
    /// The idempotency key for this logical request. Callers own the key's lifetime — it must be
    /// stable across Polly's transport-level retries and rotated only once a response (success or
    /// business failure) actually arrives, never per HTTP send.
    /// </param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Guid>> PlaceOrderAsync(Guid idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Confirms one of the caller's own orders.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> ConfirmOrderAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Cancels one of the caller's own orders.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> CancelOrderAsync(Guid id, CancellationToken cancellationToken);
}
