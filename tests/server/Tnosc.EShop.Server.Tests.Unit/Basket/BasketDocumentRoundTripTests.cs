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
using Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Tests.Unit.Basket;

/// <summary>
/// <c>Basket</c> ⇄ <see cref="BasketDocument"/> must round-trip every field, including each line's
/// snapshotted price — the whole point of the snapshot is that it survives storage and reconstruction
/// unchanged, independent of whatever the live catalogue price is by the time it is read back.
/// </summary>
public sealed class BasketDocumentRoundTripTests
{
    private readonly Faker _faker = BasketFaker.New();

    [Fact]
    public void ToDocument_Then_ToBasket_Should_PreserveEveryField_IncludingSnapshotPrices()
    {
        // Arrange
        Guid customerId = _faker.CustomerId();
        var original = BasketAggregate.CreateFor(customerId: customerId);

        Guid firstProductId = _faker.ProductId();
        string firstSku = _faker.Sku();
        string firstName = _faker.ProductName();
        Money firstPrice = Money.Create(amount: 12.34m, currency: "EUR").Value;
        original.AddItem(productId: firstProductId, sku: firstSku, productName: firstName, unitPrice: firstPrice, quantity: Quantity.Create(value: 3).Value);

        Guid secondProductId = _faker.ProductId();
        string secondSku = _faker.Sku();
        string secondName = _faker.ProductName();
        Money secondPrice = Money.Create(amount: 56.78m, currency: "EUR").Value;
        original.AddItem(productId: secondProductId, sku: secondSku, productName: secondName, unitPrice: secondPrice, quantity: Quantity.Create(value: 1).Value);

        // Act
        BasketDocument document = original.ToDocument();
        var rehydrated = document.ToBasket();

        // Assert
        rehydrated.Id.ShouldBe(expected: original.Id);
        rehydrated.CustomerId.ShouldBe(expected: original.CustomerId);
        rehydrated.Version.ShouldBe(expected: original.Version);
        rehydrated.Items.Count.ShouldBe(expected: original.Items.Count);

        BasketItem originalFirst = original.Items.Single(predicate: item => item.ProductId == firstProductId);
        BasketItem rehydratedFirst = rehydrated.Items.Single(predicate: item => item.ProductId == firstProductId);
        rehydratedFirst.Id.ShouldBe(expected: originalFirst.Id);
        rehydratedFirst.Sku.ShouldBe(expected: firstSku);
        rehydratedFirst.ProductName.ShouldBe(expected: firstName);
        rehydratedFirst.UnitPrice.Amount.ShouldBe(expected: firstPrice.Amount);
        rehydratedFirst.UnitPrice.Currency.ShouldBe(expected: firstPrice.Currency);
        rehydratedFirst.Quantity.Value.ShouldBe(expected: 3);

        BasketItem originalSecond = original.Items.Single(predicate: item => item.ProductId == secondProductId);
        BasketItem rehydratedSecond = rehydrated.Items.Single(predicate: item => item.ProductId == secondProductId);
        rehydratedSecond.Id.ShouldBe(expected: originalSecond.Id);
        rehydratedSecond.UnitPrice.Amount.ShouldBe(expected: secondPrice.Amount);
        rehydratedSecond.Quantity.Value.ShouldBe(expected: 1);

        rehydrated.Total.ShouldNotBeNull();
        rehydrated.Total.Amount.ShouldBe(expected: original.Total!.Amount);
    }

    [Fact]
    public void ToDocument_Then_ToBasket_Should_RoundTripAnEmptyBasket()
    {
        // Arrange
        var original = BasketAggregate.CreateFor(customerId: _faker.CustomerId());

        // Act
        var rehydrated = original.ToDocument().ToBasket();

        // Assert
        rehydrated.Items.ShouldBeEmpty();
        rehydrated.Version.ShouldBe(expected: original.Version);
    }
}
