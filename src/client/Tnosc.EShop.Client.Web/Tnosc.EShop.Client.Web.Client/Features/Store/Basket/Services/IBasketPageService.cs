// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Basket.ViewModels;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Basket.Services;

/// <summary>
/// <see cref="Pages.BasketPage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IBasketApi"/> for the basket page.
/// </summary>
public interface IBasketPageService
{
    /// <summary>Reads the caller's own basket.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<BasketViewModel>> GetBasketAsync(CancellationToken cancellationToken);

    /// <summary>Changes a basket item's quantity.</summary>
    /// <param name="itemId">The basket item to change.</param>
    /// <param name="quantity">The new quantity.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<BasketViewModel>> ChangeQuantityAsync(Guid itemId, int quantity, CancellationToken cancellationToken);

    /// <summary>Removes an item from the caller's basket.</summary>
    /// <param name="itemId">The basket item to remove.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> RemoveItemAsync(Guid itemId, CancellationToken cancellationToken);
}
