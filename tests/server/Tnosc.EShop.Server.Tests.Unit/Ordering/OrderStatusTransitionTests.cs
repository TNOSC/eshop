// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using Shouldly;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Events;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// The status machine, legal paths and illegal ones, exercised entirely through the aggregate — which
/// is the only thing in the solution allowed to know it.
/// </summary>
public sealed class OrderStatusTransitionTests
{
    [Fact]
    public void Create_Should_LeaveTheOrderPending()
    {
        // Act
        Order order = OrderTestFactory.Pending();

        // Assert
        order.Status.ShouldBe(expected: OrderStatus.Pending);
    }

    [Fact]
    public void Confirm_Should_MoveAPendingOrderToConfirmed_And_RaiseTheEvent()
    {
        // Arrange
        Order order = OrderTestFactory.Pending();

        // Act
        Result confirmed = order.Confirm();

        // Assert
        confirmed.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Confirmed);
        order.DomainEvents.OfType<OrderConfirmedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void MarkPaid_Should_MoveAConfirmedOrderToPaid_And_RaiseTheEvent()
    {
        // Arrange
        Order order = OrderTestFactory.Confirmed();

        // Act
        Result paid = order.MarkPaid();

        // Assert
        paid.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Paid);
        order.DomainEvents.OfType<OrderPaidDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Ship_Should_MoveAPaidOrderToShipped()
    {
        // Arrange
        Order order = OrderTestFactory.Paid();

        // Act
        Result shipped = order.Ship();

        // Assert
        shipped.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Shipped);
    }

    [Fact]
    public void Deliver_Should_MoveAShippedOrderToDelivered()
    {
        // Arrange
        Order order = OrderTestFactory.Shipped();

        // Act
        Result delivered = order.Deliver();

        // Assert
        delivered.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Delivered);
    }

    [Fact]
    public void Cancel_Should_MoveAPendingOrderToCancelled_And_RaiseTheEvent()
    {
        // Arrange
        Order order = OrderTestFactory.Pending();

        // Act
        Result cancelled = order.Cancel();

        // Assert
        cancelled.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Cancelled);
        order.DomainEvents.OfType<OrderCancelledDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Cancel_Should_BeReachable_From_Confirmed_And_Paid()
    {
        // Act
        Result fromConfirmed = OrderTestFactory.Confirmed().Cancel();
        Result fromPaid = OrderTestFactory.Paid().Cancel();

        // Assert
        fromConfirmed.IsSuccess.ShouldBeTrue(customMessage: "a confirmed order has not shipped, so it is still cancellable");
        fromPaid.IsSuccess.ShouldBeTrue(customMessage: "a paid order has not shipped, so it is still cancellable");
    }

    [Fact]
    public void Ship_Should_ReturnConflict_When_TheOrderHasNotBeenPaidFor()
    {
        // Arrange
        Order order = OrderTestFactory.Confirmed();

        // Act
        Result shipped = order.Ship();

        // Assert
        // The case the plan singles out: shipping an unpaid order is a Conflict, which the endpoint
        // maps to 409 — not a 400, and certainly not a silent success.
        shipped.IsError.ShouldBeTrue();
        shipped.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        shipped.FirstError.Code.ShouldBe(expected: "Order.CannotShip");
        order.Status.ShouldBe(expected: OrderStatus.Confirmed, customMessage: "a refused transition must not move the order");
    }

    [Fact]
    public void Ship_Should_ReturnConflict_When_TheOrderIsStillPending()
    {
        // Act
        Result shipped = OrderTestFactory.Pending().Ship();

        // Assert
        shipped.IsError.ShouldBeTrue();
        shipped.FirstError.Code.ShouldBe(expected: "Order.CannotShip");
    }

    [Fact]
    public void Cancel_Should_ReturnConflict_When_TheOrderHasShipped()
    {
        // Arrange
        Order order = OrderTestFactory.Shipped();

        // Act
        Result cancelled = order.Cancel();

        // Assert
        cancelled.IsError.ShouldBeTrue();
        cancelled.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        cancelled.FirstError.Code.ShouldBe(expected: "Order.CannotCancel");
        order.Status.ShouldBe(expected: OrderStatus.Shipped);
    }

    [Fact]
    public void Cancel_Should_ReturnConflict_When_TheOrderHasBeenDelivered()
    {
        // Act
        Result cancelled = OrderTestFactory.Delivered().Cancel();

        // Assert
        cancelled.IsError.ShouldBeTrue();
        cancelled.FirstError.Code.ShouldBe(expected: "Order.CannotCancel");
    }

    [Fact]
    public void Cancel_Should_ReturnConflict_When_TheOrderIsAlreadyCancelled()
    {
        // Act
        Result cancelled = OrderTestFactory.Cancelled().Cancel();

        // Assert
        cancelled.IsError.ShouldBeTrue();
        cancelled.FirstError.Code.ShouldBe(expected: "Order.CannotCancel");
    }

    [Fact]
    public void Confirm_Should_ReturnConflict_When_TheOrderIsAlreadyConfirmed()
    {
        // Arrange
        Order order = OrderTestFactory.Confirmed();

        // Act
        Result confirmedAgain = order.Confirm();

        // Assert
        confirmedAgain.IsError.ShouldBeTrue();
        confirmedAgain.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        confirmedAgain.FirstError.Code.ShouldBe(expected: "Order.CannotConfirm");
        order.DomainEvents.OfType<OrderConfirmedDomainEvent>().Count()
            .ShouldBe(expected: 1, customMessage: "a refused transition must not raise a second event");
    }

    [Fact]
    public void Confirm_Should_ReturnConflict_When_TheOrderIsCancelled()
    {
        // Act
        Result confirmed = OrderTestFactory.Cancelled().Confirm();

        // Assert
        confirmed.IsError.ShouldBeTrue();
        confirmed.FirstError.Code.ShouldBe(expected: "Order.CannotConfirm");
    }

    [Fact]
    public void MarkPaid_Should_ReturnConflict_When_TheOrderIsStillPending()
    {
        // Act
        Result paid = OrderTestFactory.Pending().MarkPaid();

        // Assert
        paid.IsError.ShouldBeTrue();
        paid.FirstError.Code.ShouldBe(expected: "Order.CannotMarkPaid");
    }

    [Fact]
    public void MarkPaid_Should_ReturnConflict_When_TheOrderIsAlreadyPaid()
    {
        // Act
        Result paidAgain = OrderTestFactory.Paid().MarkPaid();

        // Assert
        paidAgain.IsError.ShouldBeTrue();
        paidAgain.FirstError.Code.ShouldBe(expected: "Order.CannotMarkPaid");
    }

    [Fact]
    public void Deliver_Should_ReturnConflict_When_TheOrderHasNotShipped()
    {
        // Act
        Result delivered = OrderTestFactory.Paid().Deliver();

        // Assert
        delivered.IsError.ShouldBeTrue();
        delivered.FirstError.Code.ShouldBe(expected: "Order.CannotDeliver");
    }

    [Fact]
    public void Transitions_Should_IncrementTheVersion_Only_When_TheySucceed()
    {
        // Arrange
        Order order = OrderTestFactory.Pending();
        int afterCreate = order.Version;

        // Act
        order.Confirm();
        int afterConfirm = order.Version;
        order.Confirm();

        // Assert
        afterCreate.ShouldBe(expected: 1, customMessage: "Create counts as a state change and increments the version");
        afterConfirm.ShouldBe(expected: afterCreate + 1);
        order.Version.ShouldBe(expected: afterConfirm, customMessage: "a refused transition changes nothing, so it must not bump the concurrency token");
    }

    [Fact]
    public void RefusedTransitions_Should_NameTheStatusTheOrderWasActuallyIn()
    {
        // Act
        Result shipped = OrderTestFactory.Pending().Ship();

        // Assert
        shipped.FirstError.Description.ShouldContain(expected: nameof(OrderStatus.Pending));
    }

    [Fact]
    public void DomainEvents_Should_CarryTheOrderIdentity_On_EveryTransitionThatRaisesOne()
    {
        // Arrange
        Order order = OrderTestFactory.Pending();
        Guid orderId = order.Id.Value;
        string orderNumber = order.Number.Value;

        // Act
        order.Confirm();
        order.MarkPaid();

        // Assert
        foreach (IDomainEvent raised in order.DomainEvents)
        {
            raised.Id.ShouldNotBe(expected: Guid.Empty, customMessage: "every event needs its own id — it is the key the inbox dedupes on");
        }

        order.DomainEvents.OfType<OrderPlacedDomainEvent>().Single().OrderId.ShouldBe(expected: orderId);
        order.DomainEvents.OfType<OrderConfirmedDomainEvent>().Single().OrderNumber.ShouldBe(expected: orderNumber);
        order.DomainEvents.OfType<OrderPaidDomainEvent>().Single().OrderId.ShouldBe(expected: orderId);
    }
}
