// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.ViewModels;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Services;

/// <summary>
/// Owns the assistant conversation: validates the shopper's draft, sends the turn, and folds the
/// streamed reply back into the view model.
/// </summary>
public interface IShoppingAssistantService
{
    /// <summary>Sends the view model's draft as the next turn and streams the reply into it.</summary>
    /// <param name="viewModel">
    /// The panel's state. Mutated in place: the draft is cleared, the shopper's turn and the
    /// assistant's reply are appended, and the reply's text grows as the stream arrives.
    /// </param>
    /// <param name="onUpdatedAsync">
    /// Invoked after each change to <paramref name="viewModel"/>, so the component can re-render mid
    /// stream. A callback rather than a returned stream keeps every mapping decision in here, where it
    /// is testable without rendering anything.
    /// </param>
    /// <param name="cancellationToken">The token observed while the reply streams.</param>
    /// <returns>
    /// A successful result once the reply has completed, or a failure carrying the problem — a
    /// rejected draft, or the agent host being unreachable — for the panel to show.
    /// </returns>
    Task<ClientResult> SendAsync(
        ShoppingAssistantViewModel viewModel,
        Func<Task> onUpdatedAsync,
        CancellationToken cancellationToken);

    /// <summary>Adds one unit of a product the assistant showed as a card to the shopper's basket.</summary>
    /// <param name="productId">The product to add, as shown on the card.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    /// <returns>
    /// A successful result carrying the basket's new item count, or a failure carrying the problem.
    /// </returns>
    Task<ClientResult<int>> AddToBasketAsync(Guid productId, CancellationToken cancellationToken);
}
