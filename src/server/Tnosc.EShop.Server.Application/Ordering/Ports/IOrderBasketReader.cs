// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Application.Ordering.Ports;

/// <summary>
/// Reads the basket an order is to be placed from.
/// </summary>
/// <remarks>
/// <para>
/// Owned by Ordering with its own snapshot types, and deliberately not a reuse of Basket's
/// <c>IBasketReader</c>: the two contexts must not reference each other, so Ordering states what it
/// needs in its own vocabulary and lets an adapter in <c>Server.Infrastructure.External</c> satisfy it.
/// The same shape as Basket's own <c>IProductLookup</c> onto Catalog — one port, one adapter, and the
/// coupling confined to that adapter rather than spread through the workflow.
/// </para>
/// <para>
/// Reading only. Ordering never clears the basket from here: that happens once the order has
/// committed, driven by <c>OrderPlacedDomainEvent</c> through the outbox, so a basket is never emptied
/// for an order that then failed to save.
/// </para>
/// </remarks>
public interface IOrderBasketReader
{
    /// <summary>
    /// Reads a customer's basket.
    /// </summary>
    /// <param name="customerId">The identifier of the customer whose basket to read.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The customer's basket, or <see langword="null"/> when they have none.</returns>
    ValueTask<OrderBasketSnapshot?> ReadAsync(Guid customerId, CancellationToken cancellationToken = default);
}
