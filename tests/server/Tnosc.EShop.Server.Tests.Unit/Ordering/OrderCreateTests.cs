// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using Shouldly;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Events;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// <see cref="Order.Create"/>: what it totals, what it rejects, and what it puts in the event the two
/// other bounded contexts will read.
/// </summary>
public sealed class OrderCreateTests
{
    [Fact]
    public void Create_Should_TotalTheLines_When_NoDiscountApplies()
    {
        // Arrange
        OrderLineDraft[] lines =
        [
            OrderTestFactory.Line(unitPrice: 10.00m, quantity: 3),
            OrderTestFactory.Line(unitPrice: 5.50m, quantity: 2),
        ];

        // Act
        Order order = OrderTestFactory.Pending(lines: lines);

        // Assert
        order.Subtotal.Amount.ShouldBe(expected: 41.00m);
        order.Total.Amount.ShouldBe(expected: 41.00m);
        order.Total.Currency.ShouldBe(expected: OrderTestFactory.DefaultCurrency);
    }

    [Fact]
    public void Create_Should_ApplyTheSuppliedStrategy_To_TheSubtotal()
    {
        // Arrange
        OrderLineDraft[] lines = [OrderTestFactory.Line(unitPrice: 100.00m, quantity: 2)];

        // Act
        Order order = OrderTestFactory.Pending(
            lines: lines,
            discountStrategy: new PercentageDiscountStrategy(percentage: 0.10m));

        // Assert
        order.Subtotal.Amount.ShouldBe(expected: 200.00m, customMessage: "the subtotal is what the lines add up to, before any discount");
        order.Total.Amount.ShouldBe(expected: 180.00m, customMessage: "the total is what the strategy left");
    }

    [Fact]
    public void Create_Should_ComputeEachLineTotal_From_UnitPriceAndQuantity()
    {
        // Act
        Order order = OrderTestFactory.Pending(lines: [OrderTestFactory.Line(unitPrice: 12.34m, quantity: 4)]);

        // Assert
        order.Lines.Single().LineTotal.Amount.ShouldBe(expected: 49.36m);
    }

    [Fact]
    public void Create_Should_GenerateAWellFormedOrderNumber()
    {
        // Act
        Order order = OrderTestFactory.Pending();

        // Assert
        order.Number.Value.Length.ShouldBe(expected: OrderNumber.Length);
        OrderNumber.Create(value: order.Number.Value).IsSuccess
            .ShouldBeTrue(customMessage: "a generated order number must satisfy the validator that parses one back");
    }

    [Fact]
    public void Create_Should_GiveTwoOrdersDistinctNumbers()
    {
        // Act
        Order first = OrderTestFactory.Pending();
        Order second = OrderTestFactory.Pending();

        // Assert
        second.Number.ShouldNotBe(expected: first.Number);
    }

    [Fact]
    public void Create_Should_RaiseOrderPlaced_CarryingEveryLine_And_TheDiscountedTotal()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        var firstProduct = Guid.CreateVersion7();
        var secondProduct = Guid.CreateVersion7();
        OrderLineDraft[] lines =
        [
            OrderTestFactory.Line(productId: firstProduct, unitPrice: 100.00m, quantity: 2),
            OrderTestFactory.Line(productId: secondProduct, unitPrice: 50.00m, quantity: 1),
        ];

        // Act
        Order order = OrderTestFactory.Pending(
            customerId: customerId,
            lines: lines,
            discountStrategy: new PercentageDiscountStrategy(percentage: 0.10m));

        // Assert
        // The two subscribers in other contexts work from this payload alone, so it has to carry the
        // customer and every line — not just the header.
        OrderPlacedDomainEvent placed = order.DomainEvents.OfType<OrderPlacedDomainEvent>().Single();

        placed.CustomerId.ShouldBe(expected: customerId);
        placed.OrderId.ShouldBe(expected: order.Id.Value);
        placed.OrderNumber.ShouldBe(expected: order.Number.Value);
        placed.TotalAmount.ShouldBe(expected: 225.00m, customMessage: "the event must carry the total after the discount, which is what the customer pays");
        placed.Lines.Length.ShouldBe(expected: 2);
        placed.Lines.ShouldContain(elementPredicate: line => line.ProductId == firstProduct && line.Quantity == 2);
        placed.Lines.ShouldContain(elementPredicate: line => line.ProductId == secondProduct && line.Quantity == 1);
    }

    [Fact]
    public void Create_Should_SnapshotTheShippingAddress_By_Value()
    {
        // Act
        Order order = OrderTestFactory.Pending();

        // Assert
        order.ShippingAddress.Street.ShouldBe(expected: "1 Rue de Carthage");
        order.ShippingAddress.Country.ShouldBe(expected: "TN");
    }

    [Fact]
    public void Create_Should_Reject_AnOrderWithNoLines()
    {
        // Act
        Result<Order> order = Order.Create(
            customerId: Guid.CreateVersion7(),
            shippingAddress: OrderTestFactory.Address(),
            lines: [],
            discountStrategy: new NoDiscountStrategy());

        // Assert
        order.IsError.ShouldBeTrue();
        order.FirstError.Type.ShouldBe(expected: ErrorType.Validation);
        order.FirstError.Code.ShouldBe(expected: "Order.NoLines");
    }

    [Fact]
    public void Create_Should_Reject_AnEmptyCustomerIdentifier()
    {
        // Act
        Result<Order> order = Order.Create(
            customerId: Guid.Empty,
            shippingAddress: OrderTestFactory.Address(),
            lines: [OrderTestFactory.Line()],
            discountStrategy: new NoDiscountStrategy());

        // Assert
        order.IsError.ShouldBeTrue();
        order.FirstError.Code.ShouldBe(expected: "Order.CustomerRequired");
    }

    [Fact]
    public void Create_Should_Reject_LinesPricedInDifferentCurrencies()
    {
        // Arrange
        OrderLineDraft[] lines =
        [
            OrderTestFactory.Line(currency: "EUR"),
            OrderTestFactory.Line(currency: "USD"),
        ];

        // Act
        Result<Order> order = Order.Create(
            customerId: Guid.CreateVersion7(),
            shippingAddress: OrderTestFactory.Address(),
            lines: lines,
            discountStrategy: new NoDiscountStrategy());

        // Assert
        order.IsError.ShouldBeTrue();
        order.FirstError.Code.ShouldBe(expected: "Order.MixedCurrencies");
    }

    [Fact]
    public void Create_Should_PropagateTheQuantityError_Unchanged()
    {
        // Act
        Result<Order> order = Order.Create(
            customerId: Guid.CreateVersion7(),
            shippingAddress: OrderTestFactory.Address(),
            lines: [OrderTestFactory.Line(quantity: 0)],
            discountStrategy: new NoDiscountStrategy());

        // Assert
        order.IsError.ShouldBeTrue();
        order.FirstError.Code.ShouldBe(expected: "OrderQuantity.OutOfRange");
    }

    [Fact]
    public void Create_Should_PropagateTheMoneyError_Unchanged()
    {
        // Act
        Result<Order> order = Order.Create(
            customerId: Guid.CreateVersion7(),
            shippingAddress: OrderTestFactory.Address(),
            lines: [OrderTestFactory.Line(unitPrice: -1.00m)],
            discountStrategy: new NoDiscountStrategy());

        // Assert
        order.IsError.ShouldBeTrue();
        order.FirstError.Code.ShouldBe(expected: "Money.NegativeAmount");
    }

    [Fact]
    public void Create_Should_Reject_ALineNamingNoProduct()
    {
        // Act
        Result<Order> order = Order.Create(
            customerId: Guid.CreateVersion7(),
            shippingAddress: OrderTestFactory.Address(),
            lines: [OrderTestFactory.Line() with { ProductId = Guid.Empty }],
            discountStrategy: new NoDiscountStrategy());

        // Assert
        order.IsError.ShouldBeTrue();
        order.FirstError.Code.ShouldBe(expected: "Order.ProductRequired");
    }

    [Fact]
    public void ApplyDiscount_Should_RepriceAPendingOrder()
    {
        // Arrange
        Order order = OrderTestFactory.Pending(lines: [OrderTestFactory.Line(unitPrice: 100.00m, quantity: 1)]);

        // Act
        Result repriced = order.ApplyDiscount(discountStrategy: new PercentageDiscountStrategy(percentage: 0.25m));

        // Assert
        repriced.IsSuccess.ShouldBeTrue();
        order.Total.Amount.ShouldBe(expected: 75.00m);
        order.Subtotal.Amount.ShouldBe(expected: 100.00m, customMessage: "the subtotal is derived from the lines and a discount must not touch it");
    }

    [Fact]
    public void ApplyDiscount_Should_ReturnConflict_When_TheOrderIsNoLongerPending()
    {
        // Arrange
        Order order = OrderTestFactory.Confirmed();
        decimal totalBefore = order.Total.Amount;

        // Act
        Result repriced = order.ApplyDiscount(discountStrategy: new PercentageDiscountStrategy(percentage: 0.50m));

        // Assert
        repriced.IsError.ShouldBeTrue();
        repriced.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        repriced.FirstError.Code.ShouldBe(expected: "Order.CannotApplyDiscount");
        order.Total.Amount.ShouldBe(expected: totalBefore, customMessage: "a confirmed total is what the customer agreed to");
    }
}
