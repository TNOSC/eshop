// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Bogus;
using NSubstitute;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// Builds valid <see cref="Product"/> instances for tests. Goes through <see cref="ProductFactory"/>
/// with a repository that reports no SKU clash, because <c>Product.Create</c> is deliberately
/// internal to the domain — the factory is the only way in, for tests as much as for handlers.
/// </summary>
internal static class ProductTestFactory
{
    /// <summary>
    /// Creates a product with random, always-valid defaults, overridable per test.
    /// </summary>
    public static async Task<Product> CreateAsync(
        string? sku = null,
        string? name = null,
        decimal? amount = null,
        string? currency = null,
        int? stock = null)
    {
        // A fresh, local Faker — xunit runs test classes in parallel, and Bogus's Randomizer is not
        // thread-safe, so this must never be a field shared across concurrently-running tests.
        Faker faker = CatalogFaker.New();
        sku ??= faker.Sku();
        name ??= faker.ProductName();
        amount ??= faker.PriceAmount();
        currency ??= faker.Currency();
        stock ??= faker.StockQuantity();

        IProductRepository repository = Substitute.For<IProductRepository>();
        repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        Result<Product> product = await ProductFactory.CreateAsync(
            repository: repository,
            sku: Sku.Create(value: sku).Value,
            name: name,
            description: null,
            price: Money.Create(amount: amount.Value, currency: currency).Value,
            stock: StockQuantity.Create(value: stock.Value).Value,
            brandId: BrandId.New(),
            categoryId: CategoryId.New());

        return product.Value;
    }
}
