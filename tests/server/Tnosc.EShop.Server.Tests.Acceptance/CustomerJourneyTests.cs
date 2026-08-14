// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shouldly;
using Tnosc.EShop.Server.Tests.Acceptance.Contracts;

namespace Tnosc.EShop.Server.Tests.Acceptance;

/// <summary>
/// The two journeys that matter, driven over HTTP against a fully booted application: a customer who
/// buys something and is charged for it, and one whose card is declined.
/// </summary>
/// <remarks>
/// Everything after <c>POST /api/orders</c> happens through the outbox — payment is opened by
/// Payment's handler for Ordering's <c>OrderPlaced</c> event, and the order's terminal status is set
/// by Ordering's handler for Payment's own event. So every assertion past that point polls
/// (<see cref="AppHostFixture.PollAsync{T}"/>) rather than sleeping.
/// <para>
/// The one step that is <em>not</em> automatic is confirmation: <c>Order.MarkPaid</c> refuses a
/// <c>Pending</c> order, so a journey that never calls <c>POST /api/orders/{id}/confirm</c> waits for a
/// <c>Paid</c> that can never arrive. The declining journey needs no confirmation, because
/// <c>Order.Cancel</c> accepts a pending order.
/// </para>
/// </remarks>
[Collection(nameof(AppHostCollection))]
public sealed class CustomerJourneyTests(AppHostFixture fixture)
{
    private const string PaidStatus = "Paid";
    private const string CancelledStatus = "Cancelled";
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    [Fact]
    public async Task Customer_Should_Reach_A_PaidOrder_When_TheJourneyRunsEndToEnd()
    {
        // Arrange — authenticate, make sure the profile and the stock the journey needs exist.
        AuthenticatedClient customer = await fixture.AuthenticateAsync(
            username: AcceptanceRoutes.CustomerUsername,
            password: AcceptanceRoutes.Password);

        AuthenticatedClient admin = await fixture.AuthenticateAsync(
            username: AcceptanceRoutes.AdminUsername,
            password: AcceptanceRoutes.Password);

        await ProvisionProfileAsync(customer: customer.Client);
        ProductSummary product = await BrowseForFeaturedProductAsync(client: customer.Client);
        await ReplenishStockAsync(admin: admin.Client, productId: product.Id);

        // Act — fill a basket and turn it into an order.
        await ResetBasketAsync(client: customer.Client);
        await AddToBasketAsync(client: customer.Client, productId: product.Id);

        Basket basket = await ReadBasketAsync(client: customer.Client);
        basket.Items.ShouldHaveSingleItem().ProductId.ShouldBe(expected: product.Id);

        Guid orderId = await PlaceOrderAsync(client: customer.Client);
        await ConfirmOrderAsync(client: customer.Client, orderId: orderId);

        // Assert — the order is paid, the payment was captured, and the basket emptied itself.
        Order paid = await WaitForOrderStatusAsync(
            client: customer.Client,
            orderId: orderId,
            status: PaidStatus,
            timeout: TimeSpan.FromMinutes(value: 3));

        paid.TotalAmount.ShouldBeGreaterThan(expected: 0m);
        paid.OrderNumber.ShouldNotBeNullOrWhiteSpace();

        Payment payment = await ReadPaymentAsync(admin: admin.Client, orderId: orderId);
        payment.Status.ShouldBe(expected: "Captured");
        payment.Method.ShouldBe(expected: "Wallet");
        payment.FailureReason.ShouldBeNull();

        await AppHostFixture.PollAsync(
            probe: async () =>
            {
                Basket current = await ReadBasketAsync(client: customer.Client);
                return current.Items.Count == 0 ? current : null;
            },
            description: "the basket to be cleared by the order-placed event");
    }

    /// <remarks>
    /// The declining card has to be presented explicitly, because the automatic reaction to
    /// <c>OrderPlaced</c> settles by wallet and always approves — see
    /// <c>OrderPlacedInitiatePaymentDomainEventHandler</c>, which documents that a card payment is
    /// driven through <c>POST /api/payments</c> instead. That means racing the outbox: the payment is
    /// posted immediately after the order is created, well inside the processor's five-second polling
    /// interval, and "one payment per order" is what makes whichever call arrives second lose. The
    /// automatic wallet attempt that loses then fails, retries, and is dead-lettered — the expected
    /// outcome of a message whose work another actor already did, and the reason this journey leaves
    /// a dead letter behind.
    /// </remarks>
    [Fact]
    public async Task Customer_Should_Reach_A_CancelledOrder_When_TheCardIsDeclined()
    {
        // Arrange
        AuthenticatedClient customer = await fixture.AuthenticateAsync(
            username: AcceptanceRoutes.CustomerUsername,
            password: AcceptanceRoutes.Password);

        AuthenticatedClient admin = await fixture.AuthenticateAsync(
            username: AcceptanceRoutes.AdminUsername,
            password: AcceptanceRoutes.Password);

        await ProvisionProfileAsync(customer: customer.Client);
        ProductSummary product = await BrowseForFeaturedProductAsync(client: customer.Client);
        await ReplenishStockAsync(admin: admin.Client, productId: product.Id);

        await ResetBasketAsync(client: customer.Client);
        await AddToBasketAsync(client: customer.Client, productId: product.Id);

        Guid orderId = await PlaceOrderAsync(client: customer.Client);
        Order placed = await ReadOrderAsync(client: customer.Client, orderId: orderId);

        // Act — pay for it with the gateway's always-declining test card.
        using HttpResponseMessage initiated = await admin.Client.PostAsJsonAsync(
            requestUri: AcceptanceRoutes.Payments,
            value: new
            {
                orderId,
                amountAmount = placed.TotalAmount,
                amountCurrency = placed.TotalCurrency,
                method = "Card",
                paymentReference = AcceptanceRoutes.DecliningCardNumber,
            });

        initiated.StatusCode.ShouldBe(
            expected: HttpStatusCode.Created,
            customMessage: "The card payment lost the race with the outbox's automatic wallet payment. "
                + "Raise OutboxOptions.PollingInterval if this becomes routine.");

        // Assert — the declined payment cancelled the order it was opened for.
        Order cancelled = await WaitForOrderStatusAsync(
            client: customer.Client,
            orderId: orderId,
            status: CancelledStatus);

        cancelled.Id.ShouldBe(expected: orderId);

        Payment payment = await ReadPaymentAsync(admin: admin.Client, orderId: orderId);
        payment.Status.ShouldBe(expected: "Failed");
        payment.Method.ShouldBe(expected: "Card");
        payment.FailureReason.ShouldBe(expected: "card_declined");
    }

    // Provisioning is the first call a client makes after logging in, and it is idempotent by design:
    // 201 the first time this realm user is seen, 200 on every later run against the same database.
    private static async Task ProvisionProfileAsync(HttpClient customer)
    {
        using HttpResponseMessage provisioned = await customer.PostAsJsonAsync(
            requestUri: AcceptanceRoutes.Customers,
            value: new { firstName = "Sami", lastName = "Shopper", phoneNumber = "+21671000001" });

        provisioned.IsSuccessStatusCode.ShouldBeTrue(
            customMessage: await AppHostFixture.DescribeAsync(response: provisioned));
        provisioned.StatusCode.ShouldBeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        // Placing an order needs a default delivery address. The first address a customer adds becomes
        // their default, so a repeat run adds a second one that changes nothing.
        using HttpResponseMessage address = await customer.PostAsJsonAsync(
            requestUri: AcceptanceRoutes.CurrentCustomerAddresses,
            value: new { street = "14 Rue de Marseille", city = "Tunis", postalCode = "1001", country = "TN" });

        address.StatusCode.ShouldBe(expected: HttpStatusCode.Created);
    }

    private static async Task<ProductSummary> BrowseForFeaturedProductAsync(HttpClient client)
    {
        PagedProducts? page = await client.GetFromJsonAsync<PagedProducts>(
            requestUri: AcceptanceRoutes.CatalogProductsPage);

        page.ShouldNotBeNull();

        return page.Items.FirstOrDefault(predicate: static product => string.Equals(
                a: product.Sku,
                b: AcceptanceRoutes.FeaturedSku,
                comparisonType: StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                message: $"The seeded catalogue holds no product with SKU {AcceptanceRoutes.FeaturedSku}.");
    }

    // The AppHost keeps its Postgres data in a volume, so the seeded stock is whatever previous runs
    // left of it. Topping it up as an admin makes each run independent of the ones before it — and
    // exercises a permissioned catalogue write on the way past.
    private static async Task ReplenishStockAsync(HttpClient admin, Guid productId)
    {
        string route = string.Create(
            provider: CultureInfo.InvariantCulture,
            handler: $"/api/catalog/products/{productId}/stock");

        using HttpResponseMessage adjusted = await admin.PostAsJsonAsync(
            requestUri: route,
            value: new { delta = 5 });

        adjusted.IsSuccessStatusCode.ShouldBeTrue(
            customMessage: $"Adjusting stock returned {(int)adjusted.StatusCode}.");
    }

    private static async Task ResetBasketAsync(HttpClient client)
    {
        using HttpResponseMessage cleared = await client.DeleteAsync(requestUri: AcceptanceRoutes.Basket);

        cleared.StatusCode.ShouldBe(expected: HttpStatusCode.NoContent);
    }

    private static async Task AddToBasketAsync(HttpClient client, Guid productId)
    {
        using HttpResponseMessage added = await client.PostAsJsonAsync(
            requestUri: AcceptanceRoutes.BasketItems,
            value: new { productId, quantity = 1 });

        added.StatusCode.ShouldBe(expected: HttpStatusCode.OK);
    }

    private static async Task<Basket> ReadBasketAsync(HttpClient client)
    {
        Basket? basket = await client.GetFromJsonAsync<Basket>(requestUri: AcceptanceRoutes.Basket);

        basket.ShouldNotBeNull();

        return basket;
    }

    // POST /api/orders takes no body — the lines, prices and address are all resolved server-side —
    // but it does take an Idempotency-Key, because its handler is [Idempotent] and the pipeline
    // rejects a request that arrives without one.
    private static async Task<Guid> PlaceOrderAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(method: HttpMethod.Post, requestUri: AcceptanceRoutes.Orders);
        request.Headers.Add(name: IdempotencyKeyHeader, value: Guid.CreateVersion7().ToString());

        using HttpResponseMessage placed = await client.SendAsync(request: request);

        placed.StatusCode.ShouldBe(
            expected: HttpStatusCode.Created,
            customMessage: await placed.Content.ReadAsStringAsync());

        return await placed.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<Order> ReadOrderAsync(HttpClient client, Guid orderId)
    {
        Order? order = await client.GetFromJsonAsync<Order>(
            requestUri: AcceptanceRoutes.OrderById(orderId: orderId));

        order.ShouldNotBeNull();

        return order;
    }

    private static async Task<Payment> ReadPaymentAsync(HttpClient admin, Guid orderId)
    {
        Payment? payment = await admin.GetFromJsonAsync<Payment>(
            requestUri: AcceptanceRoutes.PaymentByOrder(orderId: orderId));

        payment.ShouldNotBeNull();

        return payment;
    }

    // Confirming is the customer's own step, and MarkPaid refuses a Pending order — so this call is
    // what makes the wallet capture that the outbox is about to attempt able to land. It happens
    // immediately after the 201 for that reason: if the capture wins the race, its handler fails,
    // the outbox backs off, and the retry succeeds once the order is confirmed. Eventually consistent
    // either way, which is why the assertion that follows polls generously rather than assuming.
    private static async Task ConfirmOrderAsync(HttpClient client, Guid orderId)
    {
        using HttpResponseMessage confirmed = await client.PostAsync(
            requestUri: AcceptanceRoutes.OrderConfirm(orderId: orderId),
            content: null);

        confirmed.StatusCode.ShouldBe(
            expected: HttpStatusCode.NoContent,
            customMessage: await AppHostFixture.DescribeAsync(response: confirmed));
    }

    private static Task<Order> WaitForOrderStatusAsync(
        HttpClient client,
        Guid orderId,
        string status,
        TimeSpan? timeout = null) =>
        AppHostFixture.PollAsync(
            probe: async () =>
            {
                Order order = await ReadOrderAsync(client: client, orderId: orderId);

                return string.Equals(a: order.Status, b: status, comparisonType: StringComparison.Ordinal)
                    ? order
                    : null;
            },
            description: $"order {orderId} to reach {status}",
            timeout: timeout);
}
