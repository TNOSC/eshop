// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Catalog;

/// <summary>
/// The write path end to end through the real command pipeline: the aggregate is persisted, its
/// creation event lands in the outbox under its durable contract name with the whole payload, and
/// the processor drains it.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class CreateProductOutboxTests(PostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private readonly Faker _faker = CatalogFaker.New();

    [Fact]
    public async Task CreateProduct_Should_WriteAnOutboxRow_CarryingTheFullProductCreatedPayload()
    {
        // Arrange
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());
        string sku = _faker.Sku();
        string name = _faker.ProductName();
        decimal amount = _faker.PriceAmount();
        string currency = _faker.Currency();
        int stock = _faker.StockQuantity();

        // Act
        Result<ProductId> created = await CreateProductAsync(
            brand: brand,
            category: category,
            sku: sku,
            name: name,
            amount: amount,
            currency: currency,
            stock: stock);

        // Assert
        created.IsSuccess.ShouldBeTrue();

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking()
            .SingleAsync(predicate: message => message.Type == "catalog.product-created.v1");

        using var payload = JsonDocument.Parse(json: row.Content);

        payload.RootElement.GetProperty(propertyName: "ProductId").GetGuid().ShouldBe(expected: created.Value.Value);
        payload.RootElement.GetProperty(propertyName: "Sku").GetString().ShouldBe(expected: sku);
        payload.RootElement.GetProperty(propertyName: "Name").GetString().ShouldBe(expected: name);
        payload.RootElement.GetProperty(propertyName: "PriceAmount").GetDecimal().ShouldBe(expected: amount);
        payload.RootElement.GetProperty(propertyName: "PriceCurrency").GetString().ShouldBe(expected: currency);
        payload.RootElement.GetProperty(propertyName: "StockQuantity").GetInt32().ShouldBe(expected: stock);
        payload.RootElement.GetProperty(propertyName: "BrandId").GetGuid().ShouldBe(expected: brand.Id.Value);
        payload.RootElement.GetProperty(propertyName: "CategoryId").GetGuid().ShouldBe(expected: category.Id.Value);

        row.ProcessedOnUtc.ShouldBeNull();
    }

    [Fact]
    public async Task OutboxProcessor_Should_DrainTheProductCreatedRow()
    {
        // Arrange
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());
        await CreateProductAsync(brand: brand, category: category, sku: _faker.Sku());

        // Act
        int claimed = await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

        // Assert
        claimed.ShouldBe(expected: 1);

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking()
            .SingleAsync(predicate: message => message.Type == "catalog.product-created.v1");

        row.ProcessedOnUtc.ShouldNotBeNull();
        row.Error.ShouldBeNull();
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnConflict_And_WriteNothing_When_TheSkuIsAlreadyTaken()
    {
        // Arrange
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());
        string sku = _faker.Sku();
        await SeedProductAsync(sku: sku, name: _faker.ProductName(), brandId: brand.Id, categoryId: category.Id);

        // Act
        Result<ProductId> duplicate = await CreateProductAsync(brand: brand, category: category, sku: sku);

        // Assert
        duplicate.IsError.ShouldBeTrue();
        duplicate.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        duplicate.FirstError.Code.ShouldBe(expected: "Product.SkuAlreadyExists");

        WriteContext.ChangeTracker.Clear();
        (await WriteContext.Set<Product>().CountAsync(predicate: product => product.Sku.Value == sku))
            .ShouldBe(expected: 1);
    }

    private ValueTask<Result<ProductId>> CreateProductAsync(
        Brand brand,
        Category category,
        string sku,
        string? name = null,
        decimal? amount = null,
        string? currency = null,
        int? stock = null)
    {
        // CreateProductCommandHandler is [Idempotent], so each call needs its own key — exactly as
        // each HTTP request would carry its own.
        IdempotencyKeyContext.Current = Guid.CreateVersion7().ToString();

        return Scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProductCommand, ProductId>>()
            .HandleAsync(
                command: new CreateProductCommand(
                    Sku: sku,
                    Name: name ?? _faker.ProductName(),
                    Description: _faker.Description(),
                    PriceAmount: amount ?? _faker.PriceAmount(),
                    PriceCurrency: currency ?? _faker.Currency(),
                    StockQuantity: stock ?? _faker.StockQuantity(),
                    BrandId: brand.Id.Value,
                    CategoryId: category.Id.Value),
                cancellationToken: CancellationToken.None);
    }
}
