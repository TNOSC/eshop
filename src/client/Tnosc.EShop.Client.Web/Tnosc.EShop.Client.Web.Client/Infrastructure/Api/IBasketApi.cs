// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>The typed client for the Basket bounded context's API.</summary>
public interface IBasketApi
{
    /// <summary>Reads the caller's own basket. Never 404s — an empty basket is a valid response.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<BasketDto>> GetBasketAsync(CancellationToken cancellationToken);

    /// <summary>Adds an item to the caller's basket.</summary>
    /// <param name="request">The product and quantity to add.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<BasketDto>> AddItemAsync(AddItemToBasketRequest request, CancellationToken cancellationToken);

    /// <summary>Changes a basket item's quantity.</summary>
    /// <param name="itemId">The basket item to change.</param>
    /// <param name="request">The new quantity.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult<BasketDto>> ChangeItemQuantityAsync(
        Guid itemId,
        ChangeBasketItemQuantityRequest request,
        CancellationToken cancellationToken);

    /// <summary>Removes an item from the caller's basket.</summary>
    /// <param name="itemId">The basket item to remove.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> RemoveItemAsync(Guid itemId, CancellationToken cancellationToken);

    /// <summary>Clears every item from the caller's basket.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> ClearAsync(CancellationToken cancellationToken);
}
