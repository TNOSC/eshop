// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
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
using Tnosc.Lib.Application.Observabilities;
using Tnosc.Lib.Domain.Results;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Catalog;

/// <summary>
/// <c>[Idempotent]</c> on <c>CreateProductCommandHandler</c> against a real database: a retried
/// request replays its original answer and writes nothing, a key reused for different content is
/// refused, and a failed command leaves its key free.
/// </summary>
/// <remarks>
/// Every command goes through the handler resolved from DI, which is what puts the real decorator
/// chain — and therefore the real transaction — between the assertion and Postgres. Constructing the
/// handler directly would test the handler and skip the entire feature.
/// </remarks>
[Collection(nameof(PostgresCollection))]
public sealed class CreateProductIdempotencyTests(PostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private readonly Faker _faker = CatalogFaker.New();

    [Fact]
    public async Task CreateProduct_Should_ReplayTheSameProductId_And_WriteOnce_When_TheKeyIsRetried()
    {
        // Arrange
        (BrandId brandId, CategoryId categoryId) = await SeedCatalogAsync();
        CreateProductCommand command = Command(brandId: brandId, categoryId: categoryId);
        string key = NewKey();

        // Act
        Result<ProductId> first = await SendAsync(command: command, key: key);
        Result<ProductId> second = await SendAsync(command: command, key: key);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        second.Value.ShouldBe(expected: first.Value, customMessage: "a retry must replay the original product id, not mint a new one");

        (await CountProductsAsync(sku: command.Sku!)).ShouldBe(expected: 1, customMessage: "the second call must not create a second product");
        (await CountOutboxAsync()).ShouldBe(expected: 1, customMessage: "the second call must not raise a second ProductCreated event");
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnKeyReuse_When_TheSameKeyCarriesADifferentBody()
    {
        // Arrange
        (BrandId brandId, CategoryId categoryId) = await SeedCatalogAsync();
        string key = NewKey();
        await SendAsync(command: Command(brandId: brandId, categoryId: categoryId), key: key);

        // Act
        Result<ProductId> reused = await SendAsync(command: Command(brandId: brandId, categoryId: categoryId), key: key);

        // Assert
        reused.IsError.ShouldBeTrue();
        reused.FirstError.Code.ShouldBe(expected: "Idempotency.KeyReuse");
        reused.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnKeyMissing_And_WriteNothing_When_NoKeyIsSupplied()
    {
        // Arrange
        (BrandId brandId, CategoryId categoryId) = await SeedCatalogAsync();
        CreateProductCommand command = Command(brandId: brandId, categoryId: categoryId);
        IdempotencyKeyContext.Current = null;

        // Act
        Result<ProductId> result = await Handler().HandleAsync(command: command, cancellationToken: CancellationToken.None);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Idempotency.KeyMissing");
        (await CountProductsAsync(sku: command.Sku!)).ShouldBe(expected: 0);
    }

    [Fact]
    public async Task CreateProduct_Should_LeaveTheKeyFree_When_TheCommandFails()
    {
        // Arrange
        (BrandId brandId, CategoryId categoryId) = await SeedCatalogAsync();
        string sku = _faker.Sku();
        await SeedProductAsync(sku: sku, name: _faker.ProductName(), brandId: brandId, categoryId: categoryId);

        string key = NewKey();
        CreateProductCommand rejected = Command(brandId: brandId, categoryId: categoryId) with { Sku = sku };

        // Act
        Result<ProductId> failure = await SendAsync(command: rejected, key: key);

        // Assert
        failure.IsError.ShouldBeTrue();
        failure.FirstError.Code.ShouldBe(expected: "Product.SkuAlreadyExists", customMessage: "the domain's verdict must reach the caller unchanged");
        (await CountClaimsAsync()).ShouldBe(expected: 0, customMessage: "a rolled-back command must not burn its key");

        // Act
        // The same key again, this time with a SKU that is free.
        Result<ProductId> retried = await SendAsync(command: Command(brandId: brandId, categoryId: categoryId), key: key);

        // Assert
        retried.IsSuccess.ShouldBeTrue(customMessage: "a key released by a failure must be usable again");
    }

    [Fact]
    public async Task CreateProduct_Should_CreateOneProduct_When_TheSameKeyArrivesConcurrently()
    {
        // Arrange
        (BrandId brandId, CategoryId categoryId) = await SeedCatalogAsync();
        CreateProductCommand command = Command(brandId: brandId, categoryId: categoryId);
        string key = NewKey();

        // Act
        // Separate scopes, so each call gets its own DbContext, unit of work and connection — one
        // transaction has to block on the other's uncommitted claim rather than race past it.
        Task<Result<ProductId>> left = Task.Run(function: () => SendInOwnScopeAsync(command: command, key: key));
        Task<Result<ProductId>> right = Task.Run(function: () => SendInOwnScopeAsync(command: command, key: key));

        Result<ProductId>[] results = await Task.WhenAll(left, right);

        // Assert
        results.ShouldAllBe(elementPredicate: result => result.IsSuccess);
        results[1].Value.ShouldBe(expected: results[0].Value, customMessage: "both callers must be given the same product id");

        (await CountProductsAsync(sku: command.Sku!)).ShouldBe(expected: 1, customMessage: "concurrent duplicates must create exactly one product");
        (await CountOutboxAsync()).ShouldBe(expected: 1);
    }

    private async Task<(BrandId BrandId, CategoryId CategoryId)> SeedCatalogAsync()
    {
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());

        return (brand.Id, category.Id);
    }

    private CreateProductCommand Command(BrandId brandId, CategoryId categoryId) =>
        new(Sku: _faker.Sku(),
            Name: _faker.ProductName(),
            Description: _faker.Description(),
            PriceAmount: _faker.PriceAmount(),
            PriceCurrency: _faker.Currency(),
            StockQuantity: _faker.StockQuantity(),
            BrandId: brandId.Value,
            CategoryId: categoryId.Value);

    private static string NewKey() => Guid.CreateVersion7().ToString();

    private ICommandHandler<CreateProductCommand, ProductId> Handler() =>
        Scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProductCommand, ProductId>>();

    private ValueTask<Result<ProductId>> SendAsync(CreateProductCommand command, string key)
    {
        IdempotencyKeyContext.Current = key;

        return Handler().HandleAsync(command: command, cancellationToken: CancellationToken.None);
    }

    private async Task<Result<ProductId>> SendInOwnScopeAsync(CreateProductCommand command, string key)
    {
        IdempotencyKeyContext.Current = key;

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();

        return await scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateProductCommand, ProductId>>()
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
    }

    private async Task<int> CountProductsAsync(string sku)
    {
        WriteContext.ChangeTracker.Clear();

        return await WriteContext.Set<Product>().CountAsync(predicate: product => product.Sku.Value == sku);
    }

    private async Task<int> CountOutboxAsync() =>
        await WriteContext.Set<OutboxMessage>().CountAsync();

    private async Task<int> CountClaimsAsync() =>
        await WriteContext.Set<IdempotencyRequest>().CountAsync();
}
