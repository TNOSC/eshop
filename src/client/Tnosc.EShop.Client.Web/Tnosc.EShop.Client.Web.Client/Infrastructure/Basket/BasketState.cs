// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;

/// <summary>
/// The current caller's basket item count, shared between <c>BasketBadge</c> in the header and every
/// page that mutates the basket, so an add on <c>ProductDetail</c> is reflected in the header without
/// either component knowing about the other.
/// </summary>
public sealed class BasketState
{
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
}
