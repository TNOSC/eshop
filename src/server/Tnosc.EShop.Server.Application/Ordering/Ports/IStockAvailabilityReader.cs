// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Application.Ordering.Ports;

/// <summary>
/// Reads how many units of each product the catalogue currently holds.
/// </summary>
/// <remarks>
/// Reads, never writes. The stock a placed order consumes is decremented by Catalog's own
/// <c>OrderPlacedAdjustStockDomainEventHandler</c>, driven by the outbox — see
/// <c>StockReserver</c> for why the check and the decrement are deliberately separated.
/// </remarks>
public interface IStockAvailabilityReader
{
    /// <summary>
    /// Reads the stock level of each of the supplied products.
    /// </summary>
    /// <param name="productIds">The identifiers of the products to read.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The stock level of each product that exists, keyed by identifier. A product that is absent from
    /// the result does not exist in the catalogue.
    /// </returns>
    ValueTask<IReadOnlyDictionary<Guid, int>> GetStockLevelsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}
