// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;
using Tnosc.EShop.Server.Application.Catalog.Queries.GetCategories;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Queries;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Catalog;

/// <summary>
/// The two halves of the cache contract, against a real database: <c>[Cacheable]</c> on the query
/// handler serves the second call without touching Postgres, and <c>[CacheTag("catalog")]</c> on a
/// write handler makes a successful command drop that entry.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class GetCategoriesCachingTests(PostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private readonly Faker _faker = CatalogFaker.New();

    /// <summary>
    /// Projection only, against the bare handler. Constructed directly rather than resolved, so no
    /// decorator — and therefore no shared cache entry — sits between the assertion and Postgres.
    /// </summary>
    [Fact]
    public async Task HandleAsync_Should_ProjectEveryCategory_OrderedByName()
    {
        // Arrange
        string first = _faker.CategoryName();
        string second = _faker.CategoryName();
        await SeedCategoryAsync(name: first);
        await SeedCategoryAsync(name: second);

        // Act
        Result<CategoryDto[]> result = await new GetCategoriesQueryHandler(context: ReadContext)
            .HandleAsync(query: new GetCategoriesQuery(), cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        string[] expectedOrder = new[] { first, second }.OrderBy(keySelector: static name => name, comparer: StringComparer.Ordinal).ToArray();
        result.Value.Select(selector: static category => category.Name).ShouldBe(expected: expectedOrder);
    }

    [Fact]
    public async Task GetCategories_Should_ServeTheSecondCallFromCache_And_CreateProduct_Should_InvalidateIt()
    {
        // Arrange
        await ResetCatalogCacheAsync();

        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category tools = await SeedCategoryAsync(name: _faker.CategoryName());

        // Act
        // Populates the cache entry under the "catalog" tag.
        Result<CategoryDto[]> first = await GetCategoriesAsync();

        // Assert
        first.Value.Length.ShouldBe(expected: 1);

        // Arrange
        // A write that bypasses the command pipeline, so nothing invalidates the tag.
        string secondCategoryName = _faker.CategoryName();
        await SeedCategoryAsync(name: secondCategoryName);

        // Act
        Result<CategoryDto[]> second = await GetCategoriesAsync();

        // Assert
        second.Value.Length.ShouldBe(expected: 1, customMessage: "the second call must come from cache, not from Postgres");

        // Act
        // CreateProductCommandHandler carries [CacheTag("catalog")], so a successful command runs
        // CacheInvalidationDecorator and drops the entry.
        Result<ProductId> created = await CreateProductAsync(brandId: brand.Id, categoryId: tools.Id);

        // Assert
        created.IsSuccess.ShouldBeTrue();

        Result<CategoryDto[]> third = await GetCategoriesAsync();
        third.Value.Length.ShouldBe(expected: 2, customMessage: "a successful catalog command must invalidate the catalog cache tag");
        string[] expectedOrder = new[] { tools.Name, secondCategoryName }
            .OrderBy(keySelector: static name => name, comparer: StringComparer.Ordinal)
            .ToArray();
        third.Value.Select(selector: static category => category.Name).ShouldBe(expected: expectedOrder);
    }

    /// <summary>
    /// Drops any "catalog" entry an earlier run left behind — <c>HybridCache</c> is a singleton on
    /// the fixture host, so Respawn's table truncation never reaches it.
    /// </summary>
    /// <remarks>
    /// Both clock moves are load-bearing. <c>HybridCache</c> invalidates a tag by recording the
    /// instant it happened and discarding every entry created at or before it, reading that instant
    /// from the DI-registered <see cref="TimeProvider"/> — which this suite freezes and resets to
    /// wall-clock time before each test. Advancing first puts the invalidation ahead of anything
    /// cached earlier; advancing again afterwards keeps entries written from here on strictly newer
    /// than it, which is what lets them be cached at all.
    /// </remarks>
    private async Task ResetCatalogCacheAsync()
    {
        TimeProvider.Advance(delta: TimeSpan.FromMinutes(value: 10));

        await Scope.ServiceProvider.GetRequiredService<HybridCache>()
            .RemoveByTagAsync(tag: "catalog", cancellationToken: CancellationToken.None);

        TimeProvider.Advance(delta: TimeSpan.FromSeconds(value: 1));
    }

    private ValueTask<Result<CategoryDto[]>> GetCategoriesAsync() =>
        QueryHandler<GetCategoriesQuery, CategoryDto[]>().HandleAsync(
            query: new GetCategoriesQuery(),
            cancellationToken: CancellationToken.None);

    private ValueTask<Result<ProductId>> CreateProductAsync(BrandId brandId, CategoryId categoryId)
    {
        // CreateProductCommandHandler is [Idempotent], so the call needs a key — exactly as the HTTP
        // request it stands in for would carry one.
        IdempotencyKeyContext.Current = Guid.CreateVersion7().ToString();

        return Scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProductCommand, ProductId>>()
            .HandleAsync(
                command: new CreateProductCommand(
                    Sku: _faker.Sku(),
                    Name: _faker.ProductName(),
                    Description: null,
                    PriceAmount: _faker.PriceAmount(),
                    PriceCurrency: _faker.Currency(),
                    StockQuantity: _faker.StockQuantity(),
                    BrandId: brandId.Value,
                    CategoryId: categoryId.Value),
                cancellationToken: CancellationToken.None);
    }
}
