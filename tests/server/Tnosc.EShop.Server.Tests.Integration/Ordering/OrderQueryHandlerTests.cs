// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetMyOrders;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderById;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderSummary;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Ordering;

/// <summary>
/// The Ordering query handlers against real Postgres — projection correctness, paging, the ownership
/// scoping that makes another customer's order simply not exist, and the raw-SQL summary.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class OrderQueryHandlerTests(PostgresFixture fixture) : OrderingIntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetOrderById_Should_ProjectTheOrderWithItsLines()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "QUERY-1", name: "Widget", amount: 12.50m, stock: 20);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 4);
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        // Act
        Result<OrderDto> order = await GetByIdAsync(orderId: placed.Value.Value, customerId: customerId);

        // Assert
        order.IsSuccess.ShouldBeTrue();
        order.Value.Id.ShouldBe(expected: placed.Value.Value);
        order.Value.CustomerId.ShouldBe(expected: customerId);
        order.Value.Status.ShouldBe(expected: nameof(OrderStatus.Pending), customMessage: "the status column stores the enum's name, and the DTO carries it as text");
        order.Value.TotalAmount.ShouldBe(expected: 50.00m);
        order.Value.TotalCurrency.ShouldBe(expected: "EUR");
        order.Value.OrderNumber.Length.ShouldBe(expected: OrderNumber.Length);

        order.Value.ShippingStreet.ShouldBe(expected: "1 Rue de Carthage");
        order.Value.ShippingCity.ShouldBe(expected: "Tunis");
        order.Value.ShippingPostalCode.ShouldBe(expected: "1000");
        order.Value.ShippingCountry.ShouldBe(expected: "TN");

        OrderLineDto line = order.Value.Lines.ShouldHaveSingleItem();
        line.ProductId.ShouldBe(expected: product.Id.Value);
        line.Sku.ShouldBe(expected: "QUERY-1");
        line.ProductName.ShouldBe(expected: "Widget");
        line.UnitPriceAmount.ShouldBe(expected: 12.50m);
        line.Quantity.ShouldBe(expected: 4);
        line.LineTotalAmount.ShouldBe(expected: 50.00m);
    }

    [Fact]
    public async Task GetOrderById_Should_ReturnNotFound_When_NoSuchOrderExists()
    {
        // Act
        Result<OrderDto> order = await GetByIdAsync(orderId: Guid.CreateVersion7(), customerId: Guid.CreateVersion7());

        // Assert
        order.IsError.ShouldBeTrue();
        order.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        order.FirstError.Code.ShouldBe(expected: "Order.NotFound");
    }

    [Fact]
    public async Task GetOrderById_Should_ReturnNotFound_When_TheOrderBelongsToAnotherCustomer()
    {
        // Arrange
        var owner = Guid.CreateVersion7();
        var stranger = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: owner);
        Product product = await SeedProductAsync(sku: "QUERY-2", name: "Widget", amount: 10.00m, stock: 5);
        await SeedBasketItemAsync(customerId: owner, productId: product.Id.Value, quantity: 1);
        Result<OrderId> placed = await PlaceOrderAsync(customerId: owner);

        // Act
        Result<OrderDto> order = await GetByIdAsync(orderId: placed.Value.Value, customerId: stranger);

        // Assert
        // Indistinguishable from an order that does not exist, so the endpoint cannot be used to
        // discover which identifiers are real. The customer is part of the WHERE clause, not a check.
        order.IsError.ShouldBeTrue();
        order.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
    }

    [Fact]
    public async Task GetMyOrders_Should_ReturnOnlyTheCallersOrders_NewestFirst()
    {
        // Arrange
        var owner = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: owner);
        await SeedCustomerAsync(customerId: other);
        Product product = await SeedProductAsync(sku: "QUERY-3", name: "Widget", amount: 10.00m, stock: 100);

        await SeedBasketItemAsync(customerId: owner, productId: product.Id.Value, quantity: 1);
        Result<OrderId> first = await PlaceOrderAsync(customerId: owner);
        await SeedBasketItemAsync(customerId: owner, productId: product.Id.Value, quantity: 2);
        Result<OrderId> second = await PlaceOrderAsync(customerId: owner);

        await SeedBasketItemAsync(customerId: other, productId: product.Id.Value, quantity: 1);
        await PlaceOrderAsync(customerId: other);

        // Act
        Result<PagedResult<OrderSummaryDto>> page = await GetMyOrdersAsync(customerId: owner, page: 1, pageSize: 10);

        // Assert
        page.IsSuccess.ShouldBeTrue();
        page.Value.TotalCount.ShouldBe(expected: 2, customMessage: "the other customer's order must not appear");
        page.Value.Items.Select(selector: static order => order.Id)
            .ShouldBe(expected: [second.Value.Value, first.Value.Value], customMessage: "newest first");
        page.Value.Items.ShouldAllBe(elementPredicate: order => order.LineCount == 1);
    }

    [Fact]
    public async Task GetMyOrders_Should_Page_And_ClampTheRequestedPageSize()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "QUERY-4", name: "Widget", amount: 10.00m, stock: 100);

        for (int index = 0; index < 3; index++)
        {
            await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 1);
            await PlaceOrderAsync(customerId: customerId);
        }

        // Act
        Result<PagedResult<OrderSummaryDto>> firstPage = await GetMyOrdersAsync(customerId: customerId, page: 1, pageSize: 2);
        Result<PagedResult<OrderSummaryDto>> secondPage = await GetMyOrdersAsync(customerId: customerId, page: 2, pageSize: 2);
        Result<PagedResult<OrderSummaryDto>> oversized = await GetMyOrdersAsync(customerId: customerId, page: 1, pageSize: 5_000);

        // Assert
        firstPage.Value.Items.Count.ShouldBe(expected: 2);
        firstPage.Value.TotalCount.ShouldBe(expected: 3);
        firstPage.Value.TotalPages.ShouldBe(expected: 2);
        secondPage.Value.Items.Count.ShouldBe(expected: 1);
        oversized.Value.PageSize.ShouldBe(expected: 100, customMessage: "a caller must not be able to ask for an unbounded page");
    }

    [Fact]
    public async Task GetMyOrders_Should_ReturnAnEmptyPage_When_TheCustomerHasNeverOrdered()
    {
        // Act
        Result<PagedResult<OrderSummaryDto>> page = await GetMyOrdersAsync(
            customerId: Guid.CreateVersion7(),
            page: 1,
            pageSize: 10);

        // Assert
        page.IsSuccess.ShouldBeTrue();
        page.Value.Items.ShouldBeEmpty();
        page.Value.TotalCount.ShouldBe(expected: 0);
    }

    [Fact]
    public async Task GetOrderSummary_Should_RollUpTheLines_With_TheDiscountTheOrderReceived()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product first = await SeedProductAsync(sku: "QUERY-5A", name: "Widget", amount: 100.00m, stock: 50);
        Product second = await SeedProductAsync(sku: "QUERY-5B", name: "Gadget", amount: 50.00m, stock: 50);

        // 3 × 100 + 2 × 50 = 400, past the standard tier's 250 threshold, so 2% comes off.
        await SeedBasketItemAsync(customerId: customerId, productId: first.Id.Value, quantity: 3);
        await SeedBasketItemAsync(customerId: customerId, productId: second.Id.Value, quantity: 2);
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        // Act
        Result<OrderSummaryReportDto> summary = await GetSummaryAsync(orderId: placed.Value.Value);

        // Assert
        summary.IsSuccess.ShouldBeTrue();
        summary.Value.OrderId.ShouldBe(expected: placed.Value.Value);
        summary.Value.CustomerId.ShouldBe(expected: customerId);
        summary.Value.Status.ShouldBe(expected: nameof(OrderStatus.Pending));
        summary.Value.SubtotalAmount.ShouldBe(expected: 400.00m);
        summary.Value.TotalAmount.ShouldBe(expected: 392.00m);
        summary.Value.DiscountAmount.ShouldBe(expected: 8.00m);
        summary.Value.LineCount.ShouldBe(expected: 2);
        summary.Value.TotalUnits.ShouldBe(expected: 5);
    }

    [Fact]
    public async Task GetOrderSummary_Should_AgreeWithTheOrderTheLinqQueryReturns()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "QUERY-6", name: "Widget", amount: 12.50m, stock: 20);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 4);
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        // Act
        Result<OrderSummaryReportDto> summary = await GetSummaryAsync(orderId: placed.Value.Value);
        Result<OrderDto> order = await GetByIdAsync(orderId: placed.Value.Value, customerId: customerId);

        // Assert
        // The raw-SQL path and the LINQ path read the same two tables, so they must agree — that is the
        // whole reason raw SQL is allowed here rather than being a second source of truth.
        summary.Value.OrderNumber.ShouldBe(expected: order.Value.OrderNumber);
        summary.Value.TotalAmount.ShouldBe(expected: order.Value.TotalAmount);
        summary.Value.PlacedOnUtc.ShouldBe(expected: order.Value.PlacedOnUtc);
        summary.Value.LineCount.ShouldBe(expected: order.Value.Lines.Count);
        summary.Value.TotalUnits.ShouldBe(expected: order.Value.Lines.Sum(selector: static line => line.Quantity));
    }

    [Fact]
    public async Task GetOrderSummary_Should_ReturnNotFound_When_NoSuchOrderExists()
    {
        // Act
        Result<OrderSummaryReportDto> summary = await GetSummaryAsync(orderId: Guid.CreateVersion7());

        // Assert
        summary.IsError.ShouldBeTrue();
        summary.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        summary.FirstError.Code.ShouldBe(expected: "Order.NotFound");
    }

    [Fact]
    public async Task GetOrderById_Should_ReflectATransition_AfterItIsCommitted()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "QUERY-7", name: "Widget", amount: 10.00m, stock: 5);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 1);
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        Order order = (await OrderRepository.GetByIdForCustomerAsync(
            orderId: placed.Value,
            customerId: customerId))!;
        order.Confirm();
        OrderRepository.Update(aggregate: order);
        await UnitOfWork.SaveChangesAsync();

        // Act
        Result<OrderDto> read = await GetByIdAsync(orderId: placed.Value.Value, customerId: customerId);

        // Assert
        read.Value.Status.ShouldBe(expected: nameof(OrderStatus.Confirmed));
    }

    private ValueTask<Result<OrderDto>> GetByIdAsync(Guid orderId, Guid customerId) =>
        QueryHandler<GetOrderByIdQuery, OrderDto>()
            .HandleAsync(
                query: new GetOrderByIdQuery(OrderId: orderId, CustomerId: customerId),
                cancellationToken: CancellationToken.None);

    private ValueTask<Result<PagedResult<OrderSummaryDto>>> GetMyOrdersAsync(Guid customerId, int page, int pageSize) =>
        QueryHandler<GetMyOrdersQuery, PagedResult<OrderSummaryDto>>()
            .HandleAsync(
                query: new GetMyOrdersQuery(CustomerId: customerId, Page: page, PageSize: pageSize),
                cancellationToken: CancellationToken.None);

    private ValueTask<Result<OrderSummaryReportDto>> GetSummaryAsync(Guid orderId) =>
        QueryHandler<GetOrderSummaryQuery, OrderSummaryReportDto>()
            .HandleAsync(
                query: new GetOrderSummaryQuery(OrderId: orderId),
                cancellationToken: CancellationToken.None);
}
