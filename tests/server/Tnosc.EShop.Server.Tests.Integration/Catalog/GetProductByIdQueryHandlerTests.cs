// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Shouldly;
using Tnosc.EShop.Server.Application.Catalog.Queries.GetProductById;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Catalog;

/// <summary>
/// The read side against a real Postgres: every column the write model persisted comes back through
/// the read model's projection, and a missing identifier is a <c>NotFound</c> result rather than an
/// exception or a null.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class GetProductByIdQueryHandlerTests(PostgresFixture fixture) : CatalogIntegrationTestBase(fixture)
{
    private readonly Faker _faker = CatalogFaker.New();

    [Fact]
    public async Task HandleAsync_Should_ProjectEveryColumn_When_TheProductExists()
    {
        // Arrange
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());
        string sku = _faker.Sku();
        string name = _faker.ProductName();
        decimal amount = _faker.PriceAmount();
        string currency = _faker.Currency();
        int stock = _faker.StockQuantity();
        string description = _faker.Description();
        Product product = await SeedProductAsync(
            sku: sku,
            name: name,
            brandId: brand.Id,
            categoryId: category.Id,
            amount: amount,
            currency: currency,
            stock: stock,
            description: description);

        // Act
        Result<ProductDto> result = await HandleAsync(productId: product.Id.Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ProductDto dto = result.Value;
        dto.Id.ShouldBe(expected: product.Id.Value);
        dto.Sku.ShouldBe(expected: sku);
        dto.Name.ShouldBe(expected: name);
        dto.Description.ShouldBe(expected: description);
        dto.PriceAmount.ShouldBe(expected: amount);
        dto.PriceCurrency.ShouldBe(expected: currency);
        dto.StockQuantity.ShouldBe(expected: stock);
        dto.BrandId.ShouldBe(expected: brand.Id.Value);
        dto.BrandName.ShouldBe(expected: brand.Name);
        dto.CategoryId.ShouldBe(expected: category.Id.Value);
        dto.IsDiscontinued.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_NoProductCarriesThatId()
    {
        // Act
        Result<ProductDto> result = await HandleAsync(productId: Guid.CreateVersion7());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Product.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_ReflectALaterWrite_When_ThePriceChangedAfterCreation()
    {
        // Arrange
        Brand brand = await SeedBrandAsync(name: _faker.BrandName());
        Category category = await SeedCategoryAsync(name: _faker.CategoryName());
        Product product = await SeedProductAsync(
            sku: _faker.Sku(),
            name: _faker.ProductName(),
            brandId: brand.Id,
            categoryId: category.Id,
            amount: _faker.PriceAmount());

        decimal newAmount = _faker.PriceAmount();
        const string newCurrency = "USD";
        product.ChangePrice(newPrice: Money.Create(amount: newAmount, currency: newCurrency).Value);
        ProductRepository.Update(aggregate: product);
        await UnitOfWork.SaveChangesAsync();

        // Act
        Result<ProductDto> result = await HandleAsync(productId: product.Id.Value);

        // Assert
        result.Value.PriceAmount.ShouldBe(expected: newAmount);
        result.Value.PriceCurrency.ShouldBe(expected: newCurrency);
    }

    private ValueTask<Result<ProductDto>> HandleAsync(Guid productId) =>
        QueryHandler<GetProductByIdQuery, ProductDto>().HandleAsync(
            query: new GetProductByIdQuery(ProductId: productId),
            cancellationToken: CancellationToken.None);
}
