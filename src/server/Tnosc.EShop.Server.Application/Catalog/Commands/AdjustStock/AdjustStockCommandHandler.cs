// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.AdjustStock;

/// <summary>
/// Loads the product, hands the transition to <see cref="Product.AdjustStock"/> and commits.
/// </summary>
/// <remarks>
/// Carries <c>[Retry(3)]</c> because two concurrent adjustments race on the aggregate's
/// concurrency token and the loser's <c>ConflictException</c> is worth retrying. Each retried attempt
/// re-runs the whole handler, including its own <c>SaveChangesAsync</c>, and EF gives that call its own
/// implicit transaction — so no <c>[Transactional]</c> is needed or wanted here.
/// </remarks>
/// <param name="repository">The product repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[Retry(3)]
[CacheTag(CacheTags.Catalog)]
internal sealed class AdjustStockCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdjustStockCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        AdjustStockCommand command,
        CancellationToken cancellationToken = default)
    {
        Product? product = await repository.GetByIdAsync(
            id: ProductId.From(value: command.ProductId),
            cancellationToken: cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(productId: command.ProductId);
        }

        Result adjusted = product.AdjustStock(delta: command.Delta);

        if (adjusted.IsError)
        {
            return adjusted.Errors.ToArray();
        }

        repository.Update(aggregate: product);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
