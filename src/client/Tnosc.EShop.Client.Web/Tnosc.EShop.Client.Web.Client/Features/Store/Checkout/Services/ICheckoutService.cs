// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Services;

/// <summary>
/// <see cref="Pages.CheckoutPage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IBasketApi"/>,
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IIdentityApi"/> and
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IOrderingApi"/> for the checkout page.
/// </summary>
public interface ICheckoutService
{
    /// <summary>Loads the caller's basket and profile together, so the page shows both or neither.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<CheckoutLoadResult> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Turns the caller's basket into an order.</summary>
    /// <param name="idempotencyKey">The idempotency key for this logical request.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<Guid>> PlaceOrderAsync(Guid idempotencyKey, CancellationToken cancellationToken);
}
