// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Ordering;

/// <summary>
/// Each <c>PlaceOrder</c> step on its own, against substituted ports — which is the point of having
/// split them out of the handler in the first place.
/// </summary>
public sealed class PlaceOrderStepTests
{
    private readonly Guid _customerId = Guid.CreateVersion7();

    [Fact]
    public async Task BasketResolver_Should_ReturnTheBasket_When_ItHasLines()
    {
        // Arrange
        IOrderBasketReader reader = Substitute.For<IOrderBasketReader>();
        var basket = new OrderBasketSnapshot(CustomerId: _customerId, Lines: [BasketLine()]);
        reader.ReadAsync(customerId: _customerId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<OrderBasketSnapshot?>(result: basket));

        // Act
        Result<OrderBasketSnapshot> result = await new BasketResolver(basketReader: reader)
            .ResolveAsync(customerId: _customerId, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: basket);
    }

    [Fact]
    public async Task BasketResolver_Should_ReturnConflict_When_TheCustomerHasNoBasket()
    {
        // Arrange
        IOrderBasketReader reader = Substitute.For<IOrderBasketReader>();
        reader.ReadAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<OrderBasketSnapshot?>(result: null));

        // Act
        Result<OrderBasketSnapshot> result = await new BasketResolver(basketReader: reader)
            .ResolveAsync(customerId: _customerId, cancellationToken: CancellationToken.None);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Order.BasketEmpty");
    }

    [Fact]
    public async Task BasketResolver_Should_ReturnConflict_When_TheBasketHasNoLines()
    {
        // Arrange
        IOrderBasketReader reader = Substitute.For<IOrderBasketReader>();
        reader.ReadAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<OrderBasketSnapshot?>(
                result: new OrderBasketSnapshot(CustomerId: _customerId, Lines: [])));

        // Act
        Result<OrderBasketSnapshot> result = await new BasketResolver(basketReader: reader)
            .ResolveAsync(customerId: _customerId, cancellationToken: CancellationToken.None);

        // Assert
        // "No basket" and "an empty basket" are the same answer to the customer.
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Order.BasketEmpty");
    }

    [Fact]
    public async Task CustomerResolver_Should_SnapshotTheDefaultAddress()
    {
        // Arrange
        ICustomerProfileReader reader = Substitute.For<ICustomerProfileReader>();
        reader.GetDefaultAddressAsync(customerId: _customerId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<CustomerProfileSnapshot?>(result: new CustomerProfileSnapshot(
                Street: "12 Avenue Habib Bourguiba",
                City: "Tunis",
                PostalCode: "1001",
                Country: "tn")));

        // Act
        Result<ShippingAddress> result = await new CustomerResolver(profileReader: reader)
            .ResolveAsync(customerId: _customerId, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Street.ShouldBe(expected: "12 Avenue Habib Bourguiba");
        result.Value.Country.ShouldBe(expected: "TN", customMessage: "the address is re-validated on the way in, which uppercases the country code");
    }

    [Fact]
    public async Task CustomerResolver_Should_ReturnConflict_When_TheCustomerHasNoDefaultAddress()
    {
        // Arrange
        ICustomerProfileReader reader = Substitute.For<ICustomerProfileReader>();
        reader.GetDefaultAddressAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<CustomerProfileSnapshot?>(result: null));

        // Act
        Result<ShippingAddress> result = await new CustomerResolver(profileReader: reader)
            .ResolveAsync(customerId: _customerId, cancellationToken: CancellationToken.None);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Order.NoShippingAddress");
    }

    [Fact]
    public async Task CustomerResolver_Should_PropagateTheValidationError_When_TheStoredAddressIsUnusable()
    {
        // Arrange
        ICustomerProfileReader reader = Substitute.For<ICustomerProfileReader>();
        reader.GetDefaultAddressAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<CustomerProfileSnapshot?>(result: new CustomerProfileSnapshot(
                Street: "  ",
                City: "Tunis",
                PostalCode: "1001",
                Country: "TN")));

        // Act
        Result<ShippingAddress> result = await new CustomerResolver(profileReader: reader)
            .ResolveAsync(customerId: _customerId, cancellationToken: CancellationToken.None);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "ShippingAddress.InvalidStreet");
    }

    [Fact]
    public async Task OrderInitializer_Should_BuildTheOrderFromTheBasket()
    {
        // Arrange
        IOrderRepository repository = Substitute.For<IOrderRepository>();
        repository.CountByCustomerIdAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult(result: 0));

        var basket = new OrderBasketSnapshot(
            CustomerId: _customerId,
            Lines: [BasketLine(unitPrice: 10.00m, quantity: 2), BasketLine(unitPrice: 5.00m, quantity: 1)]);

        // Act
        Result<Order> result = await new OrderInitializer(orderRepository: repository).InitializeAsync(
            customerId: _customerId,
            basket: basket,
            shippingAddress: OrderTestFactory.Address(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CustomerId.ShouldBe(expected: _customerId);
        result.Value.Lines.Count.ShouldBe(expected: 2);
        result.Value.Subtotal.Amount.ShouldBe(expected: 25.00m);
        result.Value.Total.Amount.ShouldBe(expected: 25.00m, customMessage: "a first-time customer under the threshold earns no discount");
    }

    [Fact]
    public async Task OrderInitializer_Should_PriceAgainstTheTierTheOrderCountEarns()
    {
        // Arrange
        IOrderRepository repository = Substitute.For<IOrderRepository>();
        repository.CountByCustomerIdAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult(result: CustomerTierFactory.GoldThreshold));

        var basket = new OrderBasketSnapshot(
            CustomerId: _customerId,
            Lines: [BasketLine(unitPrice: 100.00m, quantity: 6)]);

        // Act
        Result<Order> result = await new OrderInitializer(orderRepository: repository).InitializeAsync(
            customerId: _customerId,
            basket: basket,
            shippingAddress: OrderTestFactory.Address(),
            cancellationToken: CancellationToken.None);

        // Assert
        // A Gold customer at a 600 subtotal lands in the tiered scheme's middle band — 10% off. The
        // step chose neither the tier nor the percentage; both came from the domain's factories.
        result.IsSuccess.ShouldBeTrue();
        result.Value.Subtotal.Amount.ShouldBe(expected: 600.00m);
        result.Value.Total.Amount.ShouldBe(expected: 540.00m);
    }

    [Fact]
    public async Task OrderInitializer_Should_PropagateTheDomainsValidationError()
    {
        // Arrange
        IOrderRepository repository = Substitute.For<IOrderRepository>();
        repository.CountByCustomerIdAsync(customerId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult(result: 0));

        var basket = new OrderBasketSnapshot(
            CustomerId: _customerId,
            Lines: [BasketLine(quantity: 0)]);

        // Act
        Result<Order> result = await new OrderInitializer(orderRepository: repository).InitializeAsync(
            customerId: _customerId,
            basket: basket,
            shippingAddress: OrderTestFactory.Address(),
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "OrderQuantity.OutOfRange");
    }

    [Fact]
    public async Task StockReserver_Should_Succeed_When_EveryLineIsCovered()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        Order order = OrderTestFactory.Pending(lines: [OrderTestFactory.Line(productId: productId, quantity: 3)]);

        IStockAvailabilityReader reader = StockReader(levels: new Dictionary<Guid, int> { [productId] = 5 });

        // Act
        Result result = await new StockReserver(stockReader: reader)
            .ReserveAsync(order: order, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task StockReserver_Should_ReturnConflict_When_ALineExceedsTheStockOnHand()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        Order order = OrderTestFactory.Pending(lines: [OrderTestFactory.Line(productId: productId, quantity: 5)]);

        IStockAvailabilityReader reader = StockReader(levels: new Dictionary<Guid, int> { [productId] = 2 });

        // Act
        Result result = await new StockReserver(stockReader: reader)
            .ReserveAsync(order: order, cancellationToken: CancellationToken.None);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        result.FirstError.Code.ShouldBe(expected: "Order.InsufficientStock");
    }

    [Fact]
    public async Task StockReserver_Should_ReturnConflict_When_TheProductIsNoLongerInTheCatalogue()
    {
        // Arrange
        Order order = OrderTestFactory.Pending(lines: [OrderTestFactory.Line(quantity: 1)]);
        IStockAvailabilityReader reader = StockReader(levels: new Dictionary<Guid, int>());

        // Act
        Result result = await new StockReserver(stockReader: reader)
            .ReserveAsync(order: order, cancellationToken: CancellationToken.None);

        // Assert
        // A product missing from the catalogue read is the same problem as having none on hand.
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Order.InsufficientStock");
    }

    [Fact]
    public async Task StockReserver_Should_AskForEveryProductOnce_However_ManyLinesNameIt()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        Order order = OrderTestFactory.Pending(lines:
        [
            OrderTestFactory.Line(productId: productId, quantity: 1),
            OrderTestFactory.Line(productId: productId, quantity: 1),
        ]);

        IStockAvailabilityReader reader = StockReader(levels: new Dictionary<Guid, int> { [productId] = 10 });

        // Act
        await new StockReserver(stockReader: reader).ReserveAsync(order: order, cancellationToken: CancellationToken.None);

        // Assert
        await reader.Received(requiredNumberOfCalls: 1).GetStockLevelsAsync(
            productIds: Arg.Is<IReadOnlyCollection<Guid>>(predicate: ids => ids.Count == 1),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StockReserver_Should_LeaveTheOrderUntouched()
    {
        // Arrange
        var productId = Guid.CreateVersion7();
        Order order = OrderTestFactory.Pending(lines: [OrderTestFactory.Line(productId: productId, quantity: 1)]);
        IStockAvailabilityReader reader = StockReader(levels: new Dictionary<Guid, int> { [productId] = 4 });
        int versionBefore = order.Version;
        int eventsBefore = order.DomainEvents.Count;

        // Act
        await new StockReserver(stockReader: reader).ReserveAsync(order: order, cancellationToken: CancellationToken.None);

        // Assert
        // The step checks and nothing more — it does not decrement, and it is not a transition on the
        // order either. The decrement is Catalog's [Idempotent] event handler; doing it here as well
        // would take the units off twice per order.
        order.Version.ShouldBe(expected: versionBefore);
        order.Status.ShouldBe(expected: OrderStatus.Pending);
        order.DomainEvents.Count.ShouldBe(expected: eventsBefore);
    }

    [Fact]
    public async Task OrderPersister_Should_AddTheOrder_And_Commit()
    {
        // Arrange
        IOrderRepository repository = Substitute.For<IOrderRepository>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        Order order = OrderTestFactory.Pending(customerId: _customerId);

        // Act
        Result<OrderId> result = await new OrderPersister(orderRepository: repository, unitOfWork: unitOfWork)
            .PersistAsync(order: order, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: order.Id);
        await repository.Received(requiredNumberOfCalls: 1).AddAsync(aggregate: order, cancellationToken: Arg.Any<CancellationToken>());
        await unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private static IStockAvailabilityReader StockReader(Dictionary<Guid, int> levels)
    {
        IStockAvailabilityReader reader = Substitute.For<IStockAvailabilityReader>();
        reader.GetStockLevelsAsync(productIds: Arg.Any<IReadOnlyCollection<Guid>>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<IReadOnlyDictionary<Guid, int>>(result: levels));

        return reader;
    }

    private static OrderBasketLine BasketLine(
        Guid? productId = null,
        decimal unitPrice = 10.00m,
        int quantity = 1) =>
        new(ProductId: productId ?? Guid.CreateVersion7(),
            Sku: "WIDGET-1",
            ProductName: "Widget",
            UnitPriceAmount: unitPrice,
            UnitPriceCurrency: "EUR",
            Quantity: quantity);
}
