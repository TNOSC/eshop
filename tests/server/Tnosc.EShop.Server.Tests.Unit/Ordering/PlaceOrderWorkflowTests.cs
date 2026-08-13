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
using Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;
using Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// The workflow's own contract: run the steps in order, and stop at the first that fails.
/// </summary>
/// <remarks>
/// Short-circuiting is the property most worth pinning down here, and it is asserted the only way that
/// actually proves it — by checking that the <em>later</em> steps were never invoked. Asserting only
/// that the error came back would pass just as happily for a workflow that ran every step and returned
/// the first error at the end, which is a different, and much worse, program.
/// </remarks>
public sealed class PlaceOrderWorkflowTests
{
    private readonly IBasketResolver _basketResolver = Substitute.For<IBasketResolver>();
    private readonly ICustomerResolver _customerResolver = Substitute.For<ICustomerResolver>();
    private readonly IOrderInitializer _orderInitializer = Substitute.For<IOrderInitializer>();
    private readonly IStockReserver _stockReserver = Substitute.For<IStockReserver>();
    private readonly IOrderPersister _orderPersister = Substitute.For<IOrderPersister>();

    private readonly Guid _customerId = Guid.CreateVersion7();

    [Fact]
    public async Task ExecuteAsync_Should_RunEveryStep_And_ReturnTheOrderId_When_AllSucceed()
    {
        // Arrange
        Order order = OrderTestFactory.Pending(customerId: _customerId);
        EveryStepSucceeds(order: order);

        // Act
        Result<OrderId> result = await ExecuteAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: order.Id);

        await _basketResolver.Received(requiredNumberOfCalls: 1).ResolveAsync(customerId: _customerId, cancellationToken: Arg.Any<CancellationToken>());
        await _customerResolver.Received(requiredNumberOfCalls: 1).ResolveAsync(customerId: _customerId, cancellationToken: Arg.Any<CancellationToken>());
        await _orderInitializer.Received(requiredNumberOfCalls: 1).InitializeAsync(
            customerId: _customerId,
            basket: Arg.Any<OrderBasketSnapshot>(),
            shippingAddress: Arg.Any<ShippingAddress>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await _stockReserver.Received(requiredNumberOfCalls: 1).ReserveAsync(order: order, cancellationToken: Arg.Any<CancellationToken>());
        await _orderPersister.Received(requiredNumberOfCalls: 1).PersistAsync(order: order, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_StopAtTheBasket_And_RunNoLaterStep_When_TheBasketIsEmpty()
    {
        // Arrange
        EveryStepSucceeds(order: OrderTestFactory.Pending(customerId: _customerId));
        _basketResolver
            .ResolveAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Failed<OrderBasketSnapshot>(error: OrderErrors.BasketEmpty(customerId: _customerId)));

        // Act
        Result<OrderId> result = await ExecuteAsync();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Order.BasketEmpty");
        await NoStepAfterTheBasketRan();
    }

    [Fact]
    public async Task ExecuteAsync_Should_StopAtTheCustomer_And_RunNoLaterStep_When_ThereIsNoShippingAddress()
    {
        // Arrange
        EveryStepSucceeds(order: OrderTestFactory.Pending(customerId: _customerId));
        _customerResolver
            .ResolveAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Failed<ShippingAddress>(error: OrderErrors.NoShippingAddress(customerId: _customerId)));

        // Act
        Result<OrderId> result = await ExecuteAsync();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Order.NoShippingAddress");

        await _basketResolver.Received(requiredNumberOfCalls: 1).ResolveAsync(customerId: _customerId, cancellationToken: Arg.Any<CancellationToken>());
        await _orderInitializer.DidNotReceive().InitializeAsync(
            customerId: Arg.Any<Guid>(),
            basket: Arg.Any<OrderBasketSnapshot>(),
            shippingAddress: Arg.Any<ShippingAddress>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await _stockReserver.DidNotReceive().ReserveAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>());
        await _orderPersister.DidNotReceive().PersistAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_StopAtTheInitializer_And_NeverCheckStockOrPersist()
    {
        // Arrange
        EveryStepSucceeds(order: OrderTestFactory.Pending(customerId: _customerId));
        _orderInitializer
            .InitializeAsync(
                customerId: Arg.Any<Guid>(),
                basket: Arg.Any<OrderBasketSnapshot>(),
                shippingAddress: Arg.Any<ShippingAddress>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Failed<Order>(error: OrderErrors.MixedCurrencies));

        // Act
        Result<OrderId> result = await ExecuteAsync();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Order.MixedCurrencies");
        await _stockReserver.DidNotReceive().ReserveAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>());
        await _orderPersister.DidNotReceive().PersistAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotPersist_When_TheStockCheckFails()
    {
        // Arrange
        Order order = OrderTestFactory.Pending(customerId: _customerId);
        EveryStepSucceeds(order: order);
        _stockReserver
            .ReserveAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: FailedResult(error: OrderErrors.InsufficientStock(
                productId: Guid.CreateVersion7(),
                requested: 5,
                available: 1)));

        // Act
        Result<OrderId> result = await ExecuteAsync();

        // Assert
        // The one that costs money if it regresses: an order whose stock check failed must never reach
        // the database, because committing it would also write the OrderPlaced outbox row and take the
        // units off for good.
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Order.InsufficientStock");
        await _orderPersister.DidNotReceive().PersistAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_PropagateTheStepsError_Unchanged()
    {
        // Arrange
        EveryStepSucceeds(order: OrderTestFactory.Pending(customerId: _customerId));
        Error stepError = OrderErrors.BasketEmpty(customerId: _customerId);
        _basketResolver
            .ResolveAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: Failed<OrderBasketSnapshot>(error: stepError));

        // Act
        Result<OrderId> result = await ExecuteAsync();

        // Assert
        // The workflow composes; it must not reinterpret. Same type, same code, same wording.
        result.FirstError.Type.ShouldBe(expected: stepError.Type);
        result.FirstError.Code.ShouldBe(expected: stepError.Code);
        result.FirstError.Description.ShouldBe(expected: stepError.Description);
    }

    private void EveryStepSucceeds(Order order)
    {
        _basketResolver
            .ResolveAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Result<OrderBasketSnapshot>>(
                result: new OrderBasketSnapshot(CustomerId: _customerId, Lines: [])));

        _customerResolver
            .ResolveAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Result<ShippingAddress>>(result: OrderTestFactory.Address()));

        _orderInitializer
            .InitializeAsync(
                customerId: Arg.Any<Guid>(),
                basket: Arg.Any<OrderBasketSnapshot>(),
                shippingAddress: Arg.Any<ShippingAddress>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Result<Order>>(result: order));

        _stockReserver
            .ReserveAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult(result: Result.Success()));

        _orderPersister
            .PersistAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Result<OrderId>>(result: order.Id));
    }

    private async Task NoStepAfterTheBasketRan()
    {
        await _customerResolver.DidNotReceive().ResolveAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>());
        await _orderInitializer.DidNotReceive().InitializeAsync(
            customerId: Arg.Any<Guid>(),
            basket: Arg.Any<OrderBasketSnapshot>(),
            shippingAddress: Arg.Any<ShippingAddress>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await _stockReserver.DidNotReceive().ReserveAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>());
        await _orderPersister.DidNotReceive().PersistAsync(order: Arg.Any<Order>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    private ValueTask<Result<OrderId>> ExecuteAsync() =>
        new PlaceOrderWorkflow(
            basketResolver: _basketResolver,
            customerResolver: _customerResolver,
            orderInitializer: _orderInitializer,
            stockReserver: _stockReserver,
            orderPersister: _orderPersister)
            .ExecuteAsync(
                command: new PlaceOrderCommand(CustomerId: _customerId),
                cancellationToken: CancellationToken.None);

    private static ValueTask<Result<TValue>> Failed<TValue>(Error error) =>
        ValueTask.FromResult<Result<TValue>>(result: error);

    private static ValueTask<Result> FailedResult(Error error) =>
        ValueTask.FromResult<Result>(result: error);
}
