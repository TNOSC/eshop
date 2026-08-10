// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Bogus;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// "No two products share a SKU" is a business rule, so it lives in the domain where the repository
/// contract is reachable — not as an <c>if</c> in the command handler.
/// </summary>
public sealed class ProductFactoryTests
{
    private readonly Faker _faker = CatalogFaker.New();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();

    [Fact]
    public async Task CreateAsync_Should_ReturnConflict_When_AnotherProductAlreadyHoldsTheSku()
    {
        // Arrange
        string sku = _faker.Sku();
        Product existing = await ProductTestFactory.CreateAsync(sku: sku);
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: existing));

        // Act
        Result<Product> result = await CreateAsync(sku: sku);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Product.SkuAlreadyExists");
    }

    [Fact]
    public async Task CreateAsync_Should_Succeed_When_TheSkuIsFree()
    {
        // Arrange
        string sku = _faker.Sku();
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result<Product> result = await CreateAsync(sku: sku);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Sku.Value.ShouldBe(expected: sku);
    }

    [Fact]
    public async Task CreateAsync_Should_ConsultTheRepositoryWithTheRequestedSku()
    {
        // Arrange
        string sku = _faker.Sku();
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        await CreateAsync(sku: sku);

        // Assert
        await _repository.Received(requiredNumberOfCalls: 1).GetBySkuAsync(
            sku: Arg.Is<Sku>(predicate: value => value.Value == sku),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Should_ReturnValidationError_When_TheNameIsMissing()
    {
        // Arrange
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result<Product> result = await CreateAsync(sku: _faker.Sku(), name: "  ");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Product.NameRequired");
    }

    // Name has no wrapping value object of its own — unlike Sku, Money and StockQuantity — so this
    // guard is the only thing standing between an over-long name and the database's HasMaxLength(200)
    // constraint if Product.Create is ever reached from anywhere other than the validated command
    // pipeline (a bulk-import job, another handler, a test).
    [Fact]
    public async Task CreateAsync_Should_ReturnValidationError_When_TheNameExceedsTheMaxLength()
    {
        // Arrange
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result<Product> result = await CreateAsync(sku: _faker.Sku(), name: new string(c: 'A', count: Product.NameMaxLength + 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Product.NameTooLong");
    }

    [Fact]
    public async Task CreateAsync_Should_ReturnValidationError_When_TheDescriptionExceedsTheMaxLength()
    {
        // Arrange
        _repository
            .GetBySkuAsync(sku: Arg.Any<Sku>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Product?>(result: null));

        // Act
        Result<Product> result = await CreateAsync(
            sku: _faker.Sku(),
            description: new string(c: 'A', count: Product.DescriptionMaxLength + 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "Product.DescriptionTooLong");
    }

    private ValueTask<Result<Product>> CreateAsync(string sku, string? name = "Widget", string? description = null) =>
        ProductFactory.CreateAsync(
            repository: _repository,
            sku: Sku.Create(value: sku).Value,
            name: name,
            description: description,
            price: Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value,
            stock: StockQuantity.Create(value: _faker.StockQuantity()).Value,
            brandId: BrandId.New(),
            categoryId: CategoryId.New());
}
