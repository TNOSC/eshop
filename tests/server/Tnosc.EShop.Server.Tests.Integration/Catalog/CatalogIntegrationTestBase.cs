// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Tests.Integration.Catalog;

/// <summary>
/// Seeding helpers shared by the Catalog query-handler tests.
/// </summary>
/// <remarks>
/// Everything is seeded through the real write pipeline — the domain factory, the registered
/// repository and <c>IUnitOfWork</c> — rather than by inserting rows. That way the rows the query
/// handlers read back are the rows production would have written, audit columns and outbox included.
/// </remarks>
/// <param name="fixture">The shared Postgres fixture.</param>
public abstract class CatalogIntegrationTestBase(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>
    /// Gets the scoped product repository, sharing this test's write context.
    /// </summary>
    protected IProductRepository ProductRepository =>
        Scope.ServiceProvider.GetRequiredService<IProductRepository>();

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
    /// Persists a brand.
    /// </summary>
    /// <param name="name">The brand's display name.</param>
    /// <returns>The persisted brand.</returns>
    protected async Task<Brand> SeedBrandAsync(string name)
    {
        Brand brand = Brand.Create(name: name).Value;
        WriteContext.Add(entity: brand);
        await UnitOfWork.SaveChangesAsync();

        return brand;
    }

    /// <summary>
    /// Persists a category.
    /// </summary>
    /// <param name="name">The category's display name.</param>
    /// <returns>The persisted category.</returns>
    protected async Task<Category> SeedCategoryAsync(string name)
    {
        Category category = Category.Create(name: name).Value;
        WriteContext.Add(entity: category);
        await UnitOfWork.SaveChangesAsync();

        return category;
    }

    /// <summary>
    /// Persists a product through <see cref="ProductFactory"/> and the registered repository.
    /// </summary>
    /// <param name="sku">The product's stock-keeping unit.</param>
    /// <param name="name">The product's display name.</param>
    /// <param name="brandId">The brand the product belongs to.</param>
    /// <param name="categoryId">The category the product belongs to.</param>
    /// <param name="amount">The product's price amount.</param>
    /// <param name="currency">The product's price currency.</param>
    /// <param name="stock">The product's stock quantity.</param>
    /// <param name="description">The product's optional description.</param>
    /// <returns>The persisted product.</returns>
    protected async Task<Product> SeedProductAsync(
        string sku,
        string name,
        BrandId brandId,
        CategoryId categoryId,
        decimal amount = 10.00m,
        string currency = "EUR",
        int stock = 5,
        string? description = null)
    {
        IProductRepository repository = ProductRepository;

        Result<Product> product = await ProductFactory.CreateAsync(
            repository: repository,
            sku: Sku.Create(value: sku).Value,
            name: name,
            description: description,
            price: Money.Create(amount: amount, currency: currency).Value,
            stock: StockQuantity.Create(value: stock).Value,
            brandId: brandId,
            categoryId: categoryId);

        await repository.AddAsync(aggregate: product.Value);
        await UnitOfWork.SaveChangesAsync();

        return product.Value;
    }
}
