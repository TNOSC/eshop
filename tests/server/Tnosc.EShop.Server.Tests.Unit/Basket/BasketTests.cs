// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using Bogus;
using Shouldly;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Unit.Basket;

/// <summary>
/// <see cref="BasketAggregate"/> owns every rule about which lines it holds and how their quantities
/// move: adding an existing product increments rather than duplicates, quantity stays bounded, and
/// removing a missing line is an error rather than a silent no-op.
/// </summary>
public sealed class BasketTests
{
    private readonly Faker _faker = BasketFaker.New();

    [Fact]
    public void CreateFor_Should_ProduceAnEmptyBasket_WithVersionOne()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();

        // Act
        var basket = BasketAggregate.CreateFor(customerId: customerId);

        // Assert
        basket.CustomerId.ShouldBe(expected: customerId);
        basket.Items.ShouldBeEmpty();
        basket.Version.ShouldBe(expected: 1);
        basket.Total.ShouldBeNull();
    }

    [Fact]
    public void AddItem_Should_AddANewLine_When_TheProductIsNotAlreadyPresent()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        Guid productId = _faker.ProductId();
        Money unitPrice = Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value;

        // Act
        Result result = basket.AddItem(
            productId: productId,
            sku: _faker.Sku(),
            productName: _faker.ProductName(),
            unitPrice: unitPrice,
            quantity: Quantity.Create(value: 2).Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        basket.Items.ShouldHaveSingleItem();
        basket.Items.Single().ProductId.ShouldBe(expected: productId);
        basket.Items.Single().Quantity.Value.ShouldBe(expected: 2);
    }

    [Fact]
    public void AddItem_Should_IncrementTheExistingLine_Rather_ThanDuplicatingIt_When_TheProductIsAlreadyPresent()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        Guid productId = _faker.ProductId();
        Money unitPrice = Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value;
        string sku = _faker.Sku();
        string name = _faker.ProductName();

        basket.AddItem(productId: productId, sku: sku, productName: name, unitPrice: unitPrice, quantity: Quantity.Create(value: 2).Value);

        // Act
        Result result = basket.AddItem(productId: productId, sku: sku, productName: name, unitPrice: unitPrice, quantity: Quantity.Create(value: 3).Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        basket.Items.ShouldHaveSingleItem(customMessage: "adding an already-present product must increment the existing line, not add a second one");
        basket.Items.Single().Quantity.Value.ShouldBe(expected: 5);
    }

    [Fact]
    public void AddItem_Should_ReturnValidationError_When_TheCombinedQuantityExceedsTheMaximum()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        Guid productId = _faker.ProductId();
        Money unitPrice = Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value;
        basket.AddItem(productId: productId, sku: _faker.Sku(), productName: _faker.ProductName(), unitPrice: unitPrice, quantity: Quantity.Create(value: Quantity.MaxValue).Value);
        int versionBeforeSecondAdd = basket.Version;

        // Act
        Result result = basket.AddItem(productId: productId, sku: _faker.Sku(), productName: _faker.ProductName(), unitPrice: unitPrice, quantity: Quantity.Create(value: 1).Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Quantity.OutOfRange");
        basket.Version.ShouldBe(expected: versionBeforeSecondAdd, customMessage: "a rejected transition must not bump the version");
    }

    [Fact]
    public void ChangeItemQuantity_Should_ReplaceTheQuantity_When_TheLineExists()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        Money unitPrice = Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value;
        basket.AddItem(productId: _faker.ProductId(), sku: _faker.Sku(), productName: _faker.ProductName(), unitPrice: unitPrice, quantity: Quantity.Create(value: 1).Value);
        BasketItemId itemId = basket.Items.Single().Id;

        // Act
        Result result = basket.ChangeItemQuantity(itemId: itemId, quantity: Quantity.Create(value: 7).Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        basket.Items.Single().Quantity.Value.ShouldBe(expected: 7);
    }

    [Fact]
    public void ChangeItemQuantity_Should_ReturnNotFound_When_TheLineDoesNotExist()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        var missingItemId = BasketItemId.New();

        // Act
        Result result = basket.ChangeItemQuantity(itemId: missingItemId, quantity: Quantity.Create(value: 1).Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Basket.ItemNotFound");
    }

    [Fact]
    public void RemoveItem_Should_RemoveTheLine_When_ItExists()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        Money unitPrice = Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value;
        basket.AddItem(productId: _faker.ProductId(), sku: _faker.Sku(), productName: _faker.ProductName(), unitPrice: unitPrice, quantity: Quantity.Create(value: 1).Value);
        BasketItemId itemId = basket.Items.Single().Id;

        // Act
        Result result = basket.RemoveItem(itemId: itemId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        basket.Items.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveItem_Should_ReturnNotFound_Rather_ThanBeingASilentNoOp_When_TheLineDoesNotExist()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());

        // Act
        Result result = basket.RemoveItem(itemId: BasketItemId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Basket.ItemNotFound");
    }

    [Fact]
    public void Clear_Should_EmptyTheBasket()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        Money unitPrice = Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value;
        basket.AddItem(productId: _faker.ProductId(), sku: _faker.Sku(), productName: _faker.ProductName(), unitPrice: unitPrice, quantity: Quantity.Create(value: 1).Value);

        // Act
        basket.Clear();

        // Assert
        basket.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Total_Should_SumEveryLinesUnitPriceTimesQuantity()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());
        const string currency = "EUR";
        basket.AddItem(productId: _faker.ProductId(), sku: _faker.Sku(), productName: _faker.ProductName(), unitPrice: Money.Create(amount: 10m, currency: currency).Value, quantity: Quantity.Create(value: 2).Value);
        basket.AddItem(productId: _faker.ProductId(), sku: _faker.Sku(), productName: _faker.ProductName(), unitPrice: Money.Create(amount: 5m, currency: currency).Value, quantity: Quantity.Create(value: 3).Value);

        // Act
        Money? total = basket.Total;

        // Assert
        total.ShouldNotBeNull();
        total.Amount.ShouldBe(expected: 35m);
        total.Currency.ShouldBe(expected: currency);
    }

    [Fact]
    public void Total_Should_BeNull_When_TheBasketHasNoLines()
    {
        // Arrange
        var basket = BasketAggregate.CreateFor(customerId: _faker.CustomerId());

        // Act & Assert
        basket.Total.ShouldBeNull();
    }

    [Fact]
    public void Rehydrate_Should_RestoreEveryFieldAndTheExactStoredVersion_WithoutIncrementingIt()
    {
        // Arrange
        var id = BasketId.New();
        Guid customerId = _faker.CustomerId();
        BasketItemSnapshot line = new(
            ItemId: BasketItemId.New(),
            ProductId: _faker.ProductId(),
            Sku: _faker.Sku(),
            ProductName: _faker.ProductName(),
            UnitPrice: Money.Create(amount: _faker.PriceAmount(), currency: _faker.Currency()).Value,
            Quantity: Quantity.Create(value: 4).Value);

        // Act
        var basket = BasketAggregate.Rehydrate(id: id, customerId: customerId, items: [line], version: 42);

        // Assert
        basket.Id.ShouldBe(expected: id);
        basket.CustomerId.ShouldBe(expected: customerId);
        basket.Version.ShouldBe(expected: 42);
        basket.Items.ShouldHaveSingleItem();
        basket.Items.Single().Id.ShouldBe(expected: line.ItemId);
        basket.Items.Single().Quantity.Value.ShouldBe(expected: 4);
    }
}
