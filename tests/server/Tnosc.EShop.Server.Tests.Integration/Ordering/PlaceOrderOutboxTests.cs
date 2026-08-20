// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Shared.Results;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Ordering;

/// <summary>
/// The cross-context path end to end, against a real database and a real Redis: an order is placed,
/// its event lands in the outbox in the same transaction, and the processor delivers it to two
/// subscribers in two other bounded contexts — Basket clears the basket, Catalog decrements stock.
/// </summary>
/// <remarks>
/// This is what T4–T6 were built for, and the redelivery test at the bottom is the one that matters
/// most: delivery is at-least-once, so a stock handler that is not genuinely idempotent silently
/// double-decrements, and nothing else in the suite would notice.
/// </remarks>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class PlaceOrderOutboxTests(PostgresFixture fixture) : OrderingIntegrationTestBase(fixture)
{
    private const string OrderPlacedContractName = "ordering.order-placed.v1";

    [Fact]
    public async Task PlaceOrder_Should_WriteAnOutboxRow_CarryingTheFullOrderPlacedPayload()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "WIDGET-1", name: "Widget", amount: 12.50m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 4);

        // Act
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        // Assert
        placed.IsSuccess.ShouldBeTrue();

        OutboxMessage row = await OrderPlacedRowAsync();
        using var payload = JsonDocument.Parse(json: row.Content);

        payload.RootElement.GetProperty(propertyName: "OrderId").GetGuid().ShouldBe(expected: placed.Value.Value);
        payload.RootElement.GetProperty(propertyName: "CustomerId").GetGuid().ShouldBe(expected: customerId);
        payload.RootElement.GetProperty(propertyName: "TotalAmount").GetDecimal().ShouldBe(expected: 50.00m);
        payload.RootElement.GetProperty(propertyName: "TotalCurrency").GetString().ShouldBe(expected: "EUR");
        payload.RootElement.GetProperty(propertyName: "OrderNumber").GetString().ShouldNotBeNullOrWhiteSpace();

        JsonElement lines = payload.RootElement.GetProperty(propertyName: "Lines");
        lines.GetArrayLength().ShouldBe(expected: 1, customMessage: "the subscribers work from this payload alone, so every line has to be in it");
        lines[0].GetProperty(propertyName: "ProductId").GetGuid().ShouldBe(expected: product.Id.Value);
        lines[0].GetProperty(propertyName: "Quantity").GetInt32().ShouldBe(expected: 4);

        row.ProcessedOnUtc.ShouldBeNull(customMessage: "the row is written by the order's own transaction, not delivered by it");
    }

    [Fact]
    public async Task PlaceOrder_Should_ClearTheBasket_And_DecrementStock_Once_TheProcessorHasRun()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "WIDGET-2", name: "Widget", amount: 10.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 3);
        await DrainOutboxAsync();

        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);
        placed.IsSuccess.ShouldBeTrue();

        // Assert
        // Before the processor ticks, neither subscriber has run: the basket still holds its line and
        // the catalogue still holds every unit. Placing an order does neither of those things itself.
        (await StockOnHandAsync(productId: product.Id)).ShouldBe(expected: 10);
        (await BasketLineCountAsync(customerId: customerId)).ShouldBe(expected: 1);

        // Act
        await DrainOutboxAsync();

        // Assert
        (await StockOnHandAsync(productId: product.Id)).ShouldBe(
            expected: 7,
            customMessage: "Catalog's subscriber must take the ordered units off stock");
        (await BasketLineCountAsync(customerId: customerId)).ShouldBe(
            expected: 0,
            customMessage: "Basket's subscriber must empty the basket the order was placed from");

        OutboxMessage row = await OrderPlacedRowAsync();
        row.ProcessedOnUtc.ShouldNotBeNull();
        row.Error.ShouldBeNull();
    }

    [Fact]
    public async Task RedeliveringOrderPlaced_Should_NotDecrementStockTwice_And_Should_LeaveTheBasketCleared()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "WIDGET-3", name: "Widget", amount: 10.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 3);
        await DrainOutboxAsync();

        (await PlaceOrderAsync(customerId: customerId)).IsSuccess.ShouldBeTrue();
        await DrainOutboxAsync();

        (await StockOnHandAsync(productId: product.Id)).ShouldBe(expected: 7);
        (await StockClaimCountAsync()).ShouldBe(expected: 1, customMessage: "the first delivery must record an inbox claim for the stock handler");

        // Act
        // Exactly the state a crash between publishing and marking the message processed leaves behind
        // — the one window the processor cannot close on its own, and the reason the handler is
        // [Idempotent] rather than merely careful.
        await ResetOrderPlacedToPendingAsync();
        await DrainOutboxAsync();

        // Assert
        (await StockOnHandAsync(productId: product.Id)).ShouldBe(
            expected: 7,
            customMessage: "a redelivered OrderPlaced must not take the units off a second time");
        (await StockClaimCountAsync()).ShouldBe(
            expected: 1,
            customMessage: "the redelivery must not add a second inbox claim");

        // The basket-clearing sibling carries no [Idempotent] and genuinely does run again — a Redis
        // delete is idempotent by construction, so the assertion is the end state, not the call count.
        (await BasketLineCountAsync(customerId: customerId)).ShouldBe(expected: 0);

        OutboxMessage row = await OrderPlacedRowAsync();
        row.ProcessedOnUtc.ShouldNotBeNull(customMessage: "a skipped redelivery still counts as processed, or the row would loop forever");
        row.Error.ShouldBeNull();
    }

    [Fact]
    public async Task PlaceOrder_Should_WriteNothing_When_TheBasketIsEmpty()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        await DrainOutboxAsync();

        // Act
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        // Assert
        placed.IsError.ShouldBeTrue();
        placed.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        placed.FirstError.Code.ShouldBe(expected: "Order.BasketEmpty");

        WriteContext.ChangeTracker.Clear();
        (await WriteContext.Set<Order>().CountAsync()).ShouldBe(expected: 0);
        (await WriteContext.Set<OutboxMessage>().CountAsync(predicate: message => message.Type == OrderPlacedContractName))
            .ShouldBe(expected: 0, customMessage: "a failed workflow must not leave an event promising an order that does not exist");
    }

    [Fact]
    public async Task PlaceOrder_Should_Fail_When_TheCustomerHasNoDefaultAddress()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId, withAddress: false);
        Product product = await SeedProductAsync(sku: "WIDGET-4", name: "Widget", amount: 10.00m, stock: 5);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 1);

        // Act
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);

        // Assert
        placed.IsError.ShouldBeTrue();
        placed.FirstError.Code.ShouldBe(expected: "Order.NoShippingAddress");
    }

    [Fact]
    public async Task PlaceOrder_Should_Fail_And_LeaveStockUntouched_When_TheCatalogueCannotSupplyTheOrder()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "WIDGET-5", name: "Widget", amount: 10.00m, stock: 2);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 5);
        await DrainOutboxAsync();

        // Act
        Result<OrderId> placed = await PlaceOrderAsync(customerId: customerId);
        await DrainOutboxAsync();

        // Assert
        placed.IsError.ShouldBeTrue();
        placed.FirstError.Type.ShouldBe(expected: ErrorType.Conflict);
        placed.FirstError.Code.ShouldBe(expected: "Order.InsufficientStock");
        (await StockOnHandAsync(productId: product.Id)).ShouldBe(expected: 2);
        (await BasketLineCountAsync(customerId: customerId)).ShouldBe(
            expected: 1,
            customMessage: "a rejected order must leave the customer's basket alone");
    }

    [Fact]
    public async Task PlaceOrder_Should_ReplayTheOriginalOrder_When_TheSameIdempotencyKeyIsRetried()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        await SeedCustomerAsync(customerId: customerId);
        Product product = await SeedProductAsync(sku: "WIDGET-6", name: "Widget", amount: 10.00m, stock: 10);
        await SeedBasketItemAsync(customerId: customerId, productId: product.Id.Value, quantity: 2);
        string key = Guid.CreateVersion7().ToString();

        // Act
        Result<OrderId> first = await PlaceOrderAsync(customerId: customerId, idempotencyKey: key);
        Result<OrderId> retry = await PlaceOrderAsync(customerId: customerId, idempotencyKey: key);

        // Assert
        // A dropped connection must be retryable without charging the customer twice.
        first.IsSuccess.ShouldBeTrue();
        retry.IsSuccess.ShouldBeTrue();
        retry.Value.ShouldBe(expected: first.Value);

        WriteContext.ChangeTracker.Clear();
        (await WriteContext.Set<Order>().CountAsync(predicate: order => order.CustomerId == customerId))
            .ShouldBe(expected: 1, customMessage: "a retried key must replay the recorded response, not place a second order");
    }

    private async Task DrainOutboxAsync() =>
        await OutboxProcessor.ProcessBatchAsync(cancellationToken: CancellationToken.None);

    private async Task<OutboxMessage> OrderPlacedRowAsync()
    {
        WriteContext.ChangeTracker.Clear();

        return await WriteContext.Set<OutboxMessage>().AsNoTracking()
            .SingleAsync(predicate: message => message.Type == OrderPlacedContractName);
    }

    private async Task<int> StockOnHandAsync(ProductId productId)
    {
        WriteContext.ChangeTracker.Clear();

        Product product = await WriteContext.Set<Product>().AsNoTracking()
            .SingleAsync(predicate: candidate => candidate.Id == productId);

        return product.Stock.Value;
    }

    private async Task<int> StockClaimCountAsync()
    {
        WriteContext.ChangeTracker.Clear();

        return await WriteContext.Set<ProcessedEvent>()
            .CountAsync(predicate: claim => claim.Handler.Contains("OrderPlacedAdjustStock"));
    }

    private async Task<int> BasketLineCountAsync(Guid customerId)
    {
        BasketSnapshot? basket = await Scope.ServiceProvider.GetRequiredService<IBasketReader>()
            .ReadAsync(customerId: customerId, cancellationToken: CancellationToken.None);

        return basket?.Items.Length ?? 0;
    }

    /// <summary>
    /// Puts the processed order-placed row back into the state a crash between publish and mark would
    /// leave it in.
    /// </summary>
    private async Task ResetOrderPlacedToPendingAsync() =>
        await WriteContext.Database.ExecuteSqlRawAsync(
            sql: $"UPDATE {OutboxMessageConfiguration.SchemaName}.{OutboxMessageConfiguration.TableName} " +
                 "SET processed_on_utc = NULL, attempts = 0, next_attempt_on_utc = NULL " +
                 "WHERE type = {0}",
            parameters: OrderPlacedContractName);
}
