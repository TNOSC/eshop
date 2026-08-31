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

namespace Tnosc.EShop.Server.Application.Catalog.Commands.SetProductImage;

/// <summary>
/// Loads the product, uploads the new image through <see cref="IProductImageStorage"/> — deleting any
/// previous one first so a replace never leaves an orphaned blob — hands the resulting URL to
/// <see cref="Product.SetImage"/> and commits.
/// </summary>
/// <param name="repository">The product repository.</param>
/// <param name="imageStorage">The physical image store.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[CacheTag(CacheTags.Catalog)]
internal sealed class SetProductImageCommandHandler(
    IProductRepository repository,
    IProductImageStorage imageStorage,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetProductImageCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        SetProductImageCommand command,
        CancellationToken cancellationToken = default)
    {
        Product? product = await repository.GetByIdAsync(
            id: ProductId.From(value: command.ProductId),
            cancellationToken: cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(productId: command.ProductId);
        }

        if (product.ImageUrl is not null)
        {
            await imageStorage.DeleteAsync(imageUrl: product.ImageUrl, cancellationToken: cancellationToken);
        }

        string imageUrl = await imageStorage.UploadAsync(
            productId: command.ProductId,
            fileName: command.FileName,
            contentType: command.ContentType,
            content: command.Content,
            cancellationToken: cancellationToken);

        Result set = product.SetImage(imageUrl: imageUrl);

        if (set.IsError)
        {
            return set.Errors.ToArray();
        }

        repository.Update(aggregate: product);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
