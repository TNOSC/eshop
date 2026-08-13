// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Queries;

/// <summary>
/// Implements <see cref="IStockAvailabilityReader"/> by reading Catalog's product read model.
/// </summary>
/// <remarks>
/// <para>
/// One query for the whole order rather than one per line — an order of twenty lines is one round
/// trip. Discontinued products are excluded, so a line naming one is reported as unavailable rather
/// than as stocked, which is what a discontinued product is from an order's point of view.
/// </para>
/// <para>
/// A read port and nothing more. Ordering never decrements stock from here; that is Catalog's own
/// <c>[Idempotent]</c> domain-event handler, driven by the outbox.
/// </para>
/// </remarks>
/// <param name="context">The read context.</param>
internal sealed class StockAvailabilityReader(EShopReadDbContext context) : IStockAvailabilityReader
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<Guid, int>> GetStockLevelsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: productIds);

        Guid[] ids = [.. productIds];

        return await context.Set<ProductReadModel>()
            .Where(predicate: readModel => ids.Contains(readModel.Id) && !readModel.IsDiscontinued)
            .Select(selector: static readModel => new StockLevelRow(
                ProductId: readModel.Id,
                StockQuantity: readModel.StockQuantity))
            .ToDictionaryAsync(
                keySelector: static row => row.ProductId,
                elementSelector: static row => row.StockQuantity,
                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// One product's stock level, projected so the query selects two columns rather than a whole
    /// product row.
    /// </summary>
    /// <param name="ProductId">The product's identifier.</param>
    /// <param name="StockQuantity">The number of units on hand.</param>
    private sealed record StockLevelRow(Guid ProductId, int StockQuantity);
}
