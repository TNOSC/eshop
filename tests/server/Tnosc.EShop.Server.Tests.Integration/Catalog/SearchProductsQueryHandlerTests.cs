// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tnosc.EShop.Server.Application.Catalog.Queries.SearchProducts;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Catalog;

/// <summary>
/// The raw-SQL search: a three-table join with paging and two optional filters, parameterised
/// throughout. The equivalence test is the important one — it pins the hand-written SQL to a LINQ
/// query EF translates itself, so a drift in either one fails the build.
/// </summary>
/// <remarks>
/// Most of these tests search by, filter by, or sort by the exact name/SKU text they seed, so those
/// strings stay literal even here — randomizing them would just re-hide the relationship the test is
/// meant to pin. Values the assertions don't key off (price, stock) are drawn from
/// <see cref="CatalogFaker"/> instead.
/// </remarks>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class SearchProductsQueryHandlerTests(PostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private readonly Faker _faker = CatalogFaker.New();

    [Fact]
    public async Task HandleAsync_Should_ProjectTheJoinedBrandAndCategoryNames()
    {
        // Arrange
        string brandName = _faker.BrandName();
        string categoryName = _faker.CategoryName();
        decimal amount = _faker.PriceAmount();
        int stock = _faker.StockQuantity();
        Brand brand = await SeedBrandAsync(name: brandName);
        Category category = await SeedCategoryAsync(name: categoryName);
        await SeedProductAsync(sku: "HAMMER-1", name: "Hammer", brandId: brand.Id, categoryId: category.Id, amount: amount, stock: stock);

        // Act
        Result<PagedResult<ProductSummaryDto>> result = await HandleAsync(searchTerm: null, categoryId: null, page: 1, pageSize: 10);

        // Assert
        ProductSummaryDto summary = result.Value.Items.ShouldHaveSingleItem();
        summary.Sku.ShouldBe(expected: "HAMMER-1");
        summary.Name.ShouldBe(expected: "Hammer");
        summary.PriceAmount.ShouldBe(expected: amount);
        summary.PriceCurrency.ShouldBe(expected: "EUR");
        summary.StockQuantity.ShouldBe(expected: stock);
        summary.BrandName.ShouldBe(expected: brandName);
        summary.CategoryName.ShouldBe(expected: categoryName);
        result.Value.TotalCount.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task HandleAsync_Should_PageTheResults_And_ReportTheUnpagedTotal()
    {
        // Arrange
        await SeedFiveProductsAsync();

        // Act
        Result<PagedResult<ProductSummaryDto>> firstPage = await HandleAsync(searchTerm: null, categoryId: null, page: 1, pageSize: 2);
        Result<PagedResult<ProductSummaryDto>> secondPage = await HandleAsync(searchTerm: null, categoryId: null, page: 2, pageSize: 2);
        Result<PagedResult<ProductSummaryDto>> thirdPage = await HandleAsync(searchTerm: null, categoryId: null, page: 3, pageSize: 2);

        // Assert
        firstPage.Value.Items.Count.ShouldBe(expected: 2);
        secondPage.Value.Items.Count.ShouldBe(expected: 2);
        thirdPage.Value.Items.Count.ShouldBe(expected: 1);

        firstPage.Value.TotalCount.ShouldBe(expected: 5);
        firstPage.Value.TotalPages.ShouldBe(expected: 3);

        // Ordered by name, so the pages must not overlap.
        Names(page: firstPage).ShouldBe(expected: ["Product A", "Product B"]);
        Names(page: secondPage).ShouldBe(expected: ["Product C", "Product D"]);
        Names(page: thirdPage).ShouldBe(expected: ["Product E"]);
    }

    [Fact]
    public async Task HandleAsync_Should_FilterByFreeText_AcrossNameAndSku_CaseInsensitively()
    {
        // Arrange
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());
        await SeedProductAsync(sku: "HAMMER-1", name: "Claw Hammer", brandId: brand.Id, categoryId: category.Id);
        await SeedProductAsync(sku: "WRENCH-1", name: "Pipe Wrench", brandId: brand.Id, categoryId: category.Id);

        // Act
        Result<PagedResult<ProductSummaryDto>> byName = await HandleAsync(searchTerm: "hammer", categoryId: null, page: 1, pageSize: 10);
        Result<PagedResult<ProductSummaryDto>> bySku = await HandleAsync(searchTerm: "wrench-1", categoryId: null, page: 1, pageSize: 10);

        // Assert
        byName.Value.Items.ShouldHaveSingleItem().Sku.ShouldBe(expected: "HAMMER-1");
        bySku.Value.Items.ShouldHaveSingleItem().Sku.ShouldBe(expected: "WRENCH-1");
    }

    [Fact]
    public async Task HandleAsync_Should_FilterByCategory()
    {
        // Arrange
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category tools = await SeedCategoryAsync(name: "Tools");
        Category toys = await SeedCategoryAsync(name: "Toys");
        await SeedProductAsync(sku: "HAMMER-1", name: "Hammer", brandId: brand.Id, categoryId: tools.Id);
        await SeedProductAsync(sku: "YOYO-1", name: "Yo-yo", brandId: brand.Id, categoryId: toys.Id);

        // Act
        Result<PagedResult<ProductSummaryDto>> result = await HandleAsync(searchTerm: null, categoryId: toys.Id.Value, page: 1, pageSize: 10);

        // Assert
        result.Value.Items.ShouldHaveSingleItem().Sku.ShouldBe(expected: "YOYO-1");
        result.Value.TotalCount.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnAnEmptyPage_When_NothingMatches()
    {
        // Arrange
        await SeedFiveProductsAsync();

        // Act
        Result<PagedResult<ProductSummaryDto>> result = await HandleAsync(searchTerm: "nothing-matches-this", categoryId: null, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(expected: 0);
        result.Value.TotalPages.ShouldBe(expected: 0);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnTheSameRowsAsAnEquivalentLinqQuery()
    {
        // Arrange
        await SeedFiveProductsAsync();
        const string term = "product";

        // Act
        Result<PagedResult<ProductSummaryDto>> rawSql = await HandleAsync(searchTerm: term, categoryId: null, page: 1, pageSize: 10);

        // The same join, filter and ordering, expressed in LINQ and translated by EF Core.
        List<ProductSummaryDto> viaLinq = await (
            from product in ReadContext.Set<ProductReadModel>()
            join brand in ReadContext.Set<BrandReadModel>() on product.BrandId equals brand.Id
            join category in ReadContext.Set<CategoryReadModel>() on product.CategoryId equals category.Id
            where EF.Functions.ILike(matchExpression: product.Name, pattern: $"%{term}%")
               || EF.Functions.ILike(matchExpression: product.Sku, pattern: $"%{term}%")
            orderby product.Name, product.Id
            select new ProductSummaryDto(
                Id: product.Id,
                Sku: product.Sku,
                Name: product.Name,
                PriceAmount: product.PriceAmount,
                PriceCurrency: product.PriceCurrency,
                StockQuantity: product.StockQuantity,
                BrandName: brand.Name,
                CategoryName: category.Name,
                ImageUrl: product.ImageUrl))
            .Take(count: 10)
            .ToListAsync();

        // Assert
        rawSql.Value.Items.ShouldBe(expected: viaLinq);
    }

    [Fact]
    public async Task HandleAsync_Should_ClampAnOutOfRangePageRequest()
    {
        // Arrange
        await SeedFiveProductsAsync();

        // Act
        Result<PagedResult<ProductSummaryDto>> result = await HandleAsync(searchTerm: null, categoryId: null, page: 0, pageSize: 0);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Page.ShouldBe(expected: 1);
        result.Value.PageSize.ShouldBe(expected: 1);
        result.Value.Items.Count.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task HandleAsync_Should_TreatTheSearchTermAsData_When_ItLooksLikeSqlInjection()
    {
        // Arrange
        await SeedFiveProductsAsync();

        // Act
        Result<PagedResult<ProductSummaryDto>> result = await HandleAsync(
            searchTerm: "'; DROP TABLE catalog.products; --",
            categoryId: null,
            page: 1,
            pageSize: 10);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();

        // The table is still there, because the term never left the parameter.
        (await ReadContext.Set<ProductReadModel>().CountAsync()).ShouldBe(expected: 5);
    }

    private static IEnumerable<string> Names(Result<PagedResult<ProductSummaryDto>> page) =>
        page.Value.Items.Select(selector: static item => item.Name);

    private async Task SeedFiveProductsAsync()
    {
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());

        foreach (string suffix in new[] { "A", "B", "C", "D", "E" })
        {
            await SeedProductAsync(
                sku: $"PRODUCT-{suffix}",
                name: $"Product {suffix}",
                brandId: brand.Id,
                categoryId: category.Id,
                amount: _faker.PriceAmount(),
                stock: _faker.StockQuantity());
        }
    }

    private ValueTask<Result<PagedResult<ProductSummaryDto>>> HandleAsync(
        string? searchTerm,
        Guid? categoryId,
        int page,
        int pageSize) =>
        QueryHandler<SearchProductsQuery, PagedResult<ProductSummaryDto>>().HandleAsync(
            query: new SearchProductsQuery(SearchTerm: searchTerm, CategoryId: categoryId, Page: page, PageSize: pageSize),
            cancellationToken: CancellationToken.None);
}
