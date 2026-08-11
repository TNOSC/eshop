// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;

/// <summary>
/// Builds the command's value objects, delegates creation to <see cref="ProductFactory"/> — which
/// owns the SKU-uniqueness rule — and commits. Single aggregate, single commit, so no
/// <c>[Transactional]</c>.
/// </summary>
/// <param name="repository">The product repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[CacheTag(CacheTags.Catalog)]
internal sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, ProductId>
{
    /// <inheritdoc />
    public async ValueTask<Result<ProductId>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<Sku> sku = Sku.Create(value: command.Sku);

        if (sku.IsError)
        {
            return sku.Errors.ToArray();
        }

        Result<Money> price = Money.Create(amount: command.PriceAmount, currency: command.PriceCurrency);

        if (price.IsError)
        {
            return price.Errors.ToArray();
        }

        Result<StockQuantity> stock = StockQuantity.Create(value: command.StockQuantity);

        if (stock.IsError)
        {
            return stock.Errors.ToArray();
        }

        Result<Product> product = await ProductFactory.CreateAsync(
            repository: repository,
            sku: sku.Value,
            name: command.Name,
            description: command.Description,
            price: price.Value,
            stock: stock.Value,
            brandId: BrandId.From(value: command.BrandId),
            categoryId: CategoryId.From(value: command.CategoryId),
            cancellationToken: cancellationToken);

        if (product.IsError)
        {
            return product.Errors.ToArray();
        }

        await repository.AddAsync(aggregate: product.Value, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return product.Value.Id;
    }
}
