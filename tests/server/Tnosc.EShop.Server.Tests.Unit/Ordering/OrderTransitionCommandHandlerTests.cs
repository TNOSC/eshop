// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Ordering.Commands.CancelOrder;
using Tnosc.EShop.Server.Application.Ordering.Commands.ConfirmOrder;
using Tnosc.EShop.Server.Application.Ordering.Commands.ShipOrder;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// The three plain transition handlers: load, delegate, commit — and propagate whatever the aggregate
/// decided.
/// </summary>
public sealed class OrderTransitionCommandHandlerTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _customerId = Guid.CreateVersion7();

    [Fact]
    public async Task ConfirmOrder_Should_ConfirmTheOrder_And_Commit()
    {
        // Arrange
        Order order = OrderTestFactory.Pending(customerId: _customerId);
        CustomerOwns(order: order);

        // Act
        Result result = await ConfirmAsync(orderId: order.Id.Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Confirmed);
        _repository.Received(requiredNumberOfCalls: 1).Update(aggregate: order);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmOrder_Should_ReturnNotFound_And_NotCommit_When_TheOrderIsNotTheCallers()
    {
        // Arrange
        // The repository lookup takes the customer, so an order that is not theirs simply is not found —
        // there is no ownership check in the handler to test, which is the point.
        NoOrderMatches();
        var orderId = Guid.CreateVersion7();

        // Act
        Result result = await ConfirmAsync(orderId: orderId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Order.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmOrder_Should_PropagateTheConflict_And_NotCommit_When_TheOrderIsAlreadyConfirmed()
    {
        // Arrange
        Order order = OrderTestFactory.Confirmed(customerId: _customerId);
        CustomerOwns(order: order);

        // Act
        Result result = await ConfirmAsync(orderId: order.Id.Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Order.CannotConfirm");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelOrder_Should_CancelTheOrder_And_Commit()
    {
        // Arrange
        Order order = OrderTestFactory.Pending(customerId: _customerId);
        CustomerOwns(order: order);

        // Act
        Result result = await CancelAsync(orderId: order.Id.Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Cancelled);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelOrder_Should_PropagateTheConflict_And_NotCommit_When_TheOrderHasShipped()
    {
        // Arrange
        Order order = OrderTestFactory.Shipped(customerId: _customerId);
        CustomerOwns(order: order);

        // Act
        Result result = await CancelAsync(orderId: order.Id.Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Order.CannotCancel");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShipOrder_Should_ShipTheOrder_And_Commit_When_ItHasBeenPaidFor()
    {
        // Arrange
        Order order = OrderTestFactory.Paid(customerId: _customerId);
        AnyCallerFinds(order: order);

        // Act
        Result result = await ShipAsync(orderId: order.Id.Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(expected: OrderStatus.Shipped);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShipOrder_Should_PropagateTheConflict_And_NotCommit_When_TheOrderIsUnpaid()
    {
        // Arrange
        Order order = OrderTestFactory.Confirmed(customerId: _customerId);
        AnyCallerFinds(order: order);

        // Act
        Result result = await ShipAsync(orderId: order.Id.Value);

        // Assert
        // The 409 the plan asks to be exercised end to end, at its origin: the aggregate refuses, and
        // the handler passes the refusal through without ever looking at a status.
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Order.CannotShip");
        order.Status.ShouldBe(expected: OrderStatus.Confirmed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShipOrder_Should_ReturnNotFound_When_NoSuchOrderExists()
    {
        // Arrange
        _repository.GetByIdAsync(id: Arg.Any<OrderId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Order?>(result: null));

        // Act
        Result result = await ShipAsync(orderId: Guid.CreateVersion7());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Order.NotFound");
    }

    private void CustomerOwns(Order order) =>
        _repository
            .GetByIdForCustomerAsync(
                orderId: Arg.Any<OrderId>(),
                customerId: _customerId,
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Order?>(result: order));

    private void NoOrderMatches() =>
        _repository
            .GetByIdForCustomerAsync(
                orderId: Arg.Any<OrderId>(),
                customerId: Arg.Any<Guid>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Order?>(result: null));

    private void AnyCallerFinds(Order order) =>
        _repository
            .GetByIdAsync(id: Arg.Any<OrderId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Order?>(result: order));

    private ValueTask<Result> ConfirmAsync(Guid orderId) =>
        new ConfirmOrderCommandHandler(repository: _repository, unitOfWork: _unitOfWork)
            .HandleAsync(
                command: new ConfirmOrderCommand(OrderId: orderId, CustomerId: _customerId),
                cancellationToken: CancellationToken.None);

    private ValueTask<Result> CancelAsync(Guid orderId) =>
        new CancelOrderCommandHandler(repository: _repository, unitOfWork: _unitOfWork)
            .HandleAsync(
                command: new CancelOrderCommand(OrderId: orderId, CustomerId: _customerId),
                cancellationToken: CancellationToken.None);

    private ValueTask<Result> ShipAsync(Guid orderId) =>
        new ShipOrderCommandHandler(repository: _repository, unitOfWork: _unitOfWork)
            .HandleAsync(
                command: new ShipOrderCommand(OrderId: orderId),
                cancellationToken: CancellationToken.None);
}
