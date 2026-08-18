// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;

/// <summary>
/// <see cref="Pages.OrderDetailPage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IOrderingApi"/> for the order detail page.
/// </summary>
public interface IOrderDetailService
{
    /// <summary>Reads one of the caller's own orders, with its lines.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Order>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Confirms one of the caller's own orders.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> ConfirmOrderAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Cancels one of the caller's own orders.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> CancelOrderAsync(Guid id, CancellationToken cancellationToken);
}
