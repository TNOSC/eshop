// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Shouldly;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Catalog.Products.Events;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Catalog;

/// <summary>
/// <see cref="Product"/> owns every transition on its own state: what creation raises, when a
/// reprice is refused, and when a stock adjustment is refused.
/// </summary>
public sealed class ProductTests
{
    private readonly Faker _faker = CatalogFaker.New();

    [Fact]
    public async Task Create_Should_RaiseProductCreatedDomainEvent_CarryingTheFullPayload()
    {
        // Arrange
        string sku = _faker.Sku();
        string name = _faker.ProductName();
        decimal amount = _faker.PriceAmount();
        string currency = _faker.Currency();
        int stock = _faker.StockQuantity();

        // Act
        Product product = await ProductTestFactory.CreateAsync(sku: sku, name: name, amount: amount, currency: currency, stock: stock);

        // Assert
        ProductCreatedDomainEvent created = product.DomainEvents.OfType<ProductCreatedDomainEvent>().ShouldHaveSingleItem();

        created.ProductId.ShouldBe(expected: product.Id.Value);
        created.Sku.ShouldBe(expected: sku);
        created.Name.ShouldBe(expected: name);
        created.PriceAmount.ShouldBe(expected: amount);
        created.PriceCurrency.ShouldBe(expected: currency);
        created.StockQuantity.ShouldBe(expected: stock);
        created.BrandId.ShouldBe(expected: product.BrandId.Value);
        created.CategoryId.ShouldBe(expected: product.CategoryId.Value);
    }

    [Fact]
    public async Task Create_Should_IncrementVersion_Because_ItMutatesState()
    {
        // Arrange & Act
        Product product = await ProductTestFactory.CreateAsync();

        // Assert
        product.Version.ShouldBe(expected: 1);
    }

    [Fact]
    public async Task ChangePrice_Should_ReplaceThePriceAndRaiseProductPriceChangedDomainEvent()
    {
        // Arrange
        string currency = _faker.Currency();
        decimal oldAmount = _faker.PriceAmount();
        decimal newAmount = _faker.PriceAmount();
        Product product = await ProductTestFactory.CreateAsync(amount: oldAmount, currency: currency);
        Money newPrice = Money.Create(amount: newAmount, currency: currency).Value;

        // Act
        Result result = product.ChangePrice(newPrice: newPrice);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.Price.ShouldBe(expected: newPrice);
        product.Version.ShouldBe(expected: 2);

        ProductPriceChangedDomainEvent changed = product.DomainEvents.OfType<ProductPriceChangedDomainEvent>().ShouldHaveSingleItem();
        changed.OldAmount.ShouldBe(expected: oldAmount);
        changed.NewAmount.ShouldBe(expected: newAmount);
    }

    [Fact]
    public async Task ChangePrice_Should_ReturnConflict_When_TheProductIsDiscontinued()
    {
        // Arrange
        string currency = _faker.Currency();
        decimal amount = _faker.PriceAmount();
        Product product = await ProductTestFactory.CreateAsync(amount: amount, currency: currency);
        product.Discontinue();

        // Act
        Result result = product.ChangePrice(newPrice: Money.Create(amount: _faker.PriceAmount(), currency: currency).Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Product.Discontinued");
        product.Price.Amount.ShouldBe(expected: amount);
    }

    [Fact]
    public async Task AdjustStock_Should_MoveTheStockLevelAndRaiseProductStockAdjustedDomainEvent()
    {
        // Arrange
        int stock = _faker.Random.Int(min: 5, max: 500);
        int delta = -_faker.Random.Int(min: 1, max: stock);
        Product product = await ProductTestFactory.CreateAsync(stock: stock);

        // Act
        Result result = product.AdjustStock(delta: delta);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.Stock.Value.ShouldBe(expected: stock + delta);

        ProductStockAdjustedDomainEvent adjusted = product.DomainEvents.OfType<ProductStockAdjustedDomainEvent>().ShouldHaveSingleItem();
        adjusted.Delta.ShouldBe(expected: delta);
        adjusted.NewQuantity.ShouldBe(expected: stock + delta);
    }

    [Fact]
    public async Task AdjustStock_Should_ReturnValidationError_And_LeaveStockUntouched_When_TheResultWouldGoNegative()
    {
        // Arrange
        int stock = _faker.Random.Int(min: 1, max: 500);
        Product product = await ProductTestFactory.CreateAsync(stock: stock);
        int versionBefore = product.Version;

        // Act
        Result result = product.AdjustStock(delta: -(stock + 1));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        result.FirstError.Code.ShouldBe(expected: "StockQuantity.Negative");
        product.Stock.Value.ShouldBe(expected: stock);
        product.Version.ShouldBe(expected: versionBefore);
        product.DomainEvents.OfType<ProductStockAdjustedDomainEvent>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Discontinue_Should_WithdrawTheProduct_And_RefuseASecondCall()
    {
        // Arrange
        Product product = await ProductTestFactory.CreateAsync();

        // Act
        Result first = product.Discontinue();
        Result second = product.Discontinue();

        // Assert
        first.IsSuccess.ShouldBeTrue();
        product.IsDiscontinued.ShouldBeTrue();

        second.IsError.ShouldBeTrue();
        second.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        second.FirstError.Code.ShouldBe(expected: "Product.AlreadyDiscontinued");
    }

    [Fact]
    public async Task ClearDomainEvents_Should_EmptyTheCollection()
    {
        // Arrange
        Product product = await ProductTestFactory.CreateAsync();
        product.DomainEvents.ShouldNotBeEmpty();

        // Act
        product.ClearDomainEvents();

        // Assert
        product.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task DomainEvents_Should_CarryTheDurableContractName()
    {
        // Arrange & Act
        Product product = await ProductTestFactory.CreateAsync();

        // Assert
        IDomainEvent created = product.DomainEvents.ShouldHaveSingleItem();

        created.GetType()
            .GetCustomAttributes(attributeType: typeof(DomainEventNameAttribute), inherit: false)
            .OfType<DomainEventNameAttribute>()
            .ShouldHaveSingleItem()
            .Name
            .ShouldBe(expected: "catalog.product-created.v1");
    }
}
