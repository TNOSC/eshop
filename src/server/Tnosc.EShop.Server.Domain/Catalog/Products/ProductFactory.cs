// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// Creates products under the catalogue-wide invariants that a single <see cref="Product"/> instance
/// cannot see on its own.
/// </summary>
/// <remarks>
/// "No two products share a SKU" is a business rule, so it must not surface as an <c>if</c> in a
/// command handler. It lives here, where the repository contract is reachable; the handler only
/// propagates whatever <see cref="CreateAsync"/> returns.
/// </remarks>
public static class ProductFactory
{
    /// <summary>
    /// Creates a product, rejecting a SKU already held by another product in the catalogue.
    /// </summary>
    /// <param name="repository">The product repository consulted for the uniqueness check.</param>
    /// <param name="sku">The product's stock-keeping unit.</param>
    /// <param name="name">The product's display name.</param>
    /// <param name="description">The product's optional long-form description.</param>
    /// <param name="price">The product's initial price.</param>
    /// <param name="stock">The product's initial stock quantity.</param>
    /// <param name="brandId">The identifier of the brand the product belongs to.</param>
    /// <param name="categoryId">The identifier of the category the product belongs to.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The created product, a <c>Product.SkuAlreadyExists</c> conflict when the SKU is taken, or
    /// whatever validation error <see cref="Product.Create"/> produces.
    /// </returns>
    public static async ValueTask<Result<Product>> CreateAsync(
        IProductRepository repository,
        Sku sku,
        string? name,
        string? description,
        Money price,
        StockQuantity stock,
        BrandId brandId,
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        Product? existing = await repository.GetBySkuAsync(sku: sku, cancellationToken: cancellationToken);

        if (existing is not null)
        {
            return ProductErrors.SkuAlreadyExists(sku: sku.Value);
        }

        return Product.Create(
            sku: sku,
            name: name,
            description: description,
            price: price,
            stock: stock,
            brandId: brandId,
            categoryId: categoryId);
    }
}
