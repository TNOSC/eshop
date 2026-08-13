// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Basket.Queries;

/// <summary>
/// Implements <see cref="IProductLookup"/> by querying the read context — a genuine Postgres read,
/// which is why this stays in <c>Server.Infrastructure.Persistence</c> rather than moving to
/// <c>Server.Infrastructure.External</c> alongside the rest of Basket's infrastructure.
/// </summary>
/// <remarks>
/// Not a query handler — it implements no <c>IQueryHandler&lt;,&gt;</c>, so it is not picked up by
/// <c>InfrastructurePersistenceExtensions.AddQueries</c>'s assembly scan and is registered explicitly
/// alongside it instead.
/// </remarks>
/// <param name="context">The read context.</param>
internal sealed class ProductLookup(EShopReadDbContext context) : IProductLookup
{
    /// <inheritdoc />
    public async ValueTask<ProductSnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await context.Set<ProductReadModel>()
            .Where(predicate: readModel => readModel.Id == productId)
            .Select(selector: readModel => new ProductSnapshot(
                ProductId: readModel.Id,
                Sku: readModel.Sku,
                Name: readModel.Name,
                PriceAmount: readModel.PriceAmount,
                PriceCurrency: readModel.PriceCurrency))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
}
