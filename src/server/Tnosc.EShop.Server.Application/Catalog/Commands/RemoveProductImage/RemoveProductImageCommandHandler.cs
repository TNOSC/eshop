// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Catalog.Ports;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.RemoveProductImage;

/// <summary>
/// Loads the product, deletes its blob (if any) through <see cref="IProductImageStorage"/>, hands the
/// transition to <see cref="Product.ClearImage"/> and commits.
/// </summary>
/// <param name="repository">The product repository.</param>
/// <param name="imageStorage">The physical image store.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[CacheTag(CacheTags.Catalog)]
internal sealed class RemoveProductImageCommandHandler(
    IProductRepository repository,
    IProductImageStorage imageStorage,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveProductImageCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        RemoveProductImageCommand command,
        CancellationToken cancellationToken = default)
    {
        Product? product = await repository.GetByIdAsync(
            id: ProductId.From(value: command.ProductId),
            cancellationToken: cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(productId: command.ProductId);
        }

        if (product.ImageUrl is null)
        {
            return Result.Success();
        }

        await imageStorage.DeleteAsync(imageUrl: product.ImageUrl, cancellationToken: cancellationToken);

        Result cleared = product.ClearImage();

        if (cleared.IsError)
        {
            return cleared.Errors.ToArray();
        }

        repository.Update(aggregate: product);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
