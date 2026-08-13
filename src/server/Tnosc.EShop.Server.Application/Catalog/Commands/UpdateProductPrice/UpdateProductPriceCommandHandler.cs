// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;

/// <summary>
/// Loads the product, hands the transition to <see cref="Product.ChangePrice"/> and commits. Whether a
/// discontinued product may be repriced is the aggregate's decision, never this handler's.
/// </summary>
/// <param name="repository">The product repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[CacheTag(CacheTags.Catalog)]
internal sealed class UpdateProductPriceCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductPriceCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        UpdateProductPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<Money> price = Money.Create(amount: command.Amount, currency: command.Currency);

        if (price.IsError)
        {
            return price.Errors.ToArray();
        }

        Product? product = await repository.GetByIdAsync(
            id: ProductId.From(value: command.ProductId),
            cancellationToken: cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(productId: command.ProductId);
        }

        Result changed = product.ChangePrice(newPrice: price.Value);

        if (changed.IsError)
        {
            return changed.Errors.ToArray();
        }

        repository.Update(aggregate: product);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
