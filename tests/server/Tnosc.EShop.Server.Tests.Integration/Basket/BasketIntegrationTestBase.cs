// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using BasketRepositoryContract = Tnosc.EShop.Server.Domain.Basket.Baskets.IBasketRepository;

namespace Tnosc.EShop.Server.Tests.Integration.Basket;

/// <summary>
/// Seeding and resolution helpers shared by the Basket integration tests.
/// </summary>
/// <remarks>
/// Products are seeded through the real Catalog write pipeline — <see cref="ProductFactory"/> and the
/// registered <see cref="IProductRepository"/> — the same as <c>CatalogIntegrationTestBase</c>, so
/// <see cref="ProductLookup"/> reads back exactly what production would have written. Basket types
/// themselves are resolved directly: <see cref="BasketRepository"/> and <see cref="BasketReader"/> are
/// both the real Redis-backed implementations, registered by
/// <c>ExternalExtensions.AddInfrastructureExternal</c> against the fixture's Redis container.
/// </remarks>
/// <param name="fixture">The shared Postgres/Redis fixture.</param>
public abstract class BasketIntegrationTestBase(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>
    /// Gets the scoped, Redis-backed basket repository.
    /// </summary>
    protected BasketRepositoryContract BasketRepository =>
        Scope.ServiceProvider.GetRequiredService<BasketRepositoryContract>();

    /// <summary>
    /// Gets the scoped, Redis-backed basket reader.
    /// </summary>
    protected IBasketReader BasketReader =>
        Scope.ServiceProvider.GetRequiredService<IBasketReader>();

    /// <summary>
    /// Gets the scoped product lookup port, over the real read context.
    /// </summary>
    protected IProductLookup ProductLookup =>
        Scope.ServiceProvider.GetRequiredService<IProductLookup>();

    /// <summary>
    /// Gets the Redis connection the fixture's host registered — the same one
    /// <see cref="BasketRepository"/> and <see cref="BasketReader"/> use.
    /// </summary>
    protected IConnectionMultiplexer RedisConnection =>
        Scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

    /// <summary>
    /// Resolves a query handler through the same decorated registration production uses.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The decorated handler.</returns>
    protected IQueryHandler<TQuery, TResponse> QueryHandler<TQuery, TResponse>()
        where TQuery : IQuery<TResponse> =>
        Scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

    /// <summary>
    /// Persists a product through the real Catalog write pipeline, so <see cref="IProductLookup"/> and
    /// the Basket command handlers read back exactly what production would have written.
    /// </summary>
    /// <param name="sku">The product's stock-keeping unit.</param>
    /// <param name="name">The product's display name.</param>
    /// <param name="amount">The product's price amount.</param>
    /// <param name="currency">The product's price currency.</param>
    /// <returns>The persisted product.</returns>
    protected async Task<Product> SeedProductAsync(string sku, string name, decimal amount, string currency)
    {
        IProductRepository repository = Scope.ServiceProvider.GetRequiredService<IProductRepository>();

        Brand brand = Brand.Create(name: BasketFaker.New().BrandName()).Value;
        Category category = Category.Create(name: BasketFaker.New().CategoryName()).Value;
        WriteContext.AddRange(brand, category);
        await UnitOfWork.SaveChangesAsync();

        Result<Product> product = await ProductFactory.CreateAsync(
            repository: repository,
            sku: Sku.Create(value: sku).Value,
            name: name,
            description: null,
            price: Money.Create(amount: amount, currency: currency).Value,
            stock: StockQuantity.Create(value: 10).Value,
            brandId: brand.Id,
            categoryId: category.Id);

        await repository.AddAsync(aggregate: product.Value);
        await UnitOfWork.SaveChangesAsync();

        return product.Value;
    }
}
