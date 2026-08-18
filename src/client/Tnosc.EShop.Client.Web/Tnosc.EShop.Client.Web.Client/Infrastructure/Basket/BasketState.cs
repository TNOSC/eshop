// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;

/// <summary>
/// The current caller's basket item count, shared between <c>BasketBadge</c> in the header and every
/// page that mutates the basket, so an add on <c>ProductDetail</c> is reflected in the header without
/// either component knowing about the other. Persists <see cref="ItemCount"/> across the
/// interactive-Server-to-WebAssembly render-mode switch, so the header does not flash back to zero
/// while <c>BasketBadge</c>'s own re-fetch is still in flight after WASM takes over.
/// </summary>
/// <remarks>
/// Deliberately a classic constructor, not a primary one: restoring from
/// <see cref="PersistentComponentState"/> and registering the persisting callback are side effects a
/// primary-constructor field initializer cannot express — same reasoning as
/// <c>PersistingRevalidatingAuthenticationStateProvider</c>.
/// </remarks>
public sealed class BasketState : IDisposable
{
    private const string PersistenceKey = nameof(BasketState);

    private readonly PersistentComponentState persistentComponentState;
    private readonly PersistingComponentStateSubscription subscription;

    /// <summary>Initializes a new instance of the <see cref="BasketState"/> class.</summary>
    /// <param name="persistentComponentState">
    /// The current render's persisted component state, used to carry <see cref="ItemCount"/> across
    /// the Server-to-WebAssembly render-mode switch.
    /// </param>
    public BasketState(PersistentComponentState persistentComponentState)
    {
        this.persistentComponentState = persistentComponentState;

        if (persistentComponentState.TryTakeFromJson(key: PersistenceKey, instance: out int storedItemCount))
        {
            ItemCount = storedItemCount;
        }

        subscription = persistentComponentState.RegisterOnPersisting(
            callback: OnPersistingAsync,
            renderMode: RenderMode.InteractiveWebAssembly);
    }

    /// <summary>Gets the number of lines currently in the caller's basket.</summary>
    public int ItemCount { get; private set; }

    /// <summary>Raised whenever <see cref="ItemCount"/> changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Sets the current item count and notifies subscribers.</summary>
    /// <param name="itemCount">The basket's current line count.</param>
    public void SetItemCount(int itemCount)
    {
        ItemCount = itemCount;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose() => subscription.Dispose();

    private Task OnPersistingAsync()
    {
        persistentComponentState.PersistAsJson(key: PersistenceKey, instance: ItemCount);
        return Task.CompletedTask;
    }
}
