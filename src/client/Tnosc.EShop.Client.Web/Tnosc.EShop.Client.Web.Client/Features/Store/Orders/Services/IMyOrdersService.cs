// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.ViewModels;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;

/// <summary>
/// <see cref="Pages.MyOrdersPage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IOrderingApi"/> for the order history page.
/// </summary>
public interface IMyOrdersService
{
    /// <summary>Lists the caller's own orders, newest first.</summary>
    /// <param name="page">The requested page.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<PagedResult<OrderSummaryViewModel>>> GetMyOrdersAsync(int page, int pageSize, CancellationToken cancellationToken);
}
