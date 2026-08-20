// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Tests.Integration.Ordering;

/// <summary>
/// Seeding and resolution helpers shared by the Ordering integration tests.
/// </summary>
/// <remarks>
/// <para>
/// Everything is seeded through the real write pipelines of the three contexts an order touches —
/// <see cref="ProductFactory"/> and <see cref="IProductRepository"/> for the catalogue,
/// <see cref="CustomerFactory"/> and <see cref="ICustomerRepository"/> for the profile, and the real
/// Redis-backed Basket command handler for the basket. Nothing is inserted by hand, so the rows the
/// order is built from are the rows production would have written, audit columns and outbox included.
/// </para>
/// <para>
/// <strong>The customer key is the identity-provider subject.</strong> Basket keys its Redis document
/// on it, Ordering stores it as <c>Order.CustomerId</c>, and <c>CustomerProfileReader</c> looks the
/// profile up by <c>external_user_id</c> — so the seeded customer's external id is the string form of
/// the same <see cref="Guid"/> every helper here takes.
/// </para>
/// </remarks>
/// <param name="fixture">The shared Postgres/Redis fixture.</param>
public abstract class OrderingIntegrationTestBase(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    /// <summary>
    /// Gets the scoped order repository, sharing this test's write context.
    /// </summary>
    protected IOrderRepository OrderRepository =>
        Scope.ServiceProvider.GetRequiredService<IOrderRepository>();

    /// <summary>
    /// Resolves a query handler through the same decorated registration production uses.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>The decorated handler.</returns>
    protected IQueryHandler<TQuery, TResponse> QueryHandler<TQuery, TResponse>()
        where TQuery : IQuery<TResponse> =>
        Scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

    /// <summary>
    /// Persists a product through the real Catalog write pipeline.
    /// </summary>
    /// <param name="sku">The product's stock-keeping unit.</param>
    /// <param name="name">The product's display name.</param>
    /// <param name="amount">The product's price amount.</param>
    /// <param name="stock">The product's initial stock quantity.</param>
    /// <param name="currency">The product's price currency.</param>
    /// <returns>The persisted product.</returns>
    protected async Task<Product> SeedProductAsync(
        string sku,
        string name,
        decimal amount,
        int stock,
        string currency = "EUR")
    {
        IProductRepository repository = Scope.ServiceProvider.GetRequiredService<IProductRepository>();

        Brand brand = Brand.Create(name: $"Brand {sku}").Value;
        Category category = Category.Create(name: $"Category {sku}").Value;
        WriteContext.AddRange(brand, category);
        await UnitOfWork.SaveChangesAsync();

        Result<Product> product = await ProductFactory.CreateAsync(
            repository: repository,
            sku: Sku.Create(value: sku).Value,
            name: name,
            description: null,
            price: Money.Create(amount: amount, currency: currency).Value,
            stock: StockQuantity.Create(value: stock).Value,
            brandId: brand.Id,
            categoryId: category.Id);

        await repository.AddAsync(aggregate: product.Value);
        await UnitOfWork.SaveChangesAsync();

        return product.Value;
    }

    /// <summary>
    /// Provisions a customer through the real Identity write pipeline and gives them one address,
    /// which becomes their default.
    /// </summary>
    /// <param name="customerId">
    /// The identity-provider subject the customer is keyed on — the same value the basket and the
    /// order use.
    /// </param>
    /// <param name="withAddress">
    /// Whether to give the customer an address. Pass <see langword="false"/> to reproduce the
    /// "customer has no default shipping address" path.
    /// </param>
    /// <returns>The persisted customer.</returns>
    protected async Task<Customer> SeedCustomerAsync(Guid customerId, bool withAddress = true)
    {
        ICustomerRepository repository = Scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

        Result<CustomerProvisioning> provisioned = await CustomerFactory.ProvisionAsync(
            repository: repository,
            externalUserId: ExternalUserId.Create(value: customerId.ToString()).Value,
            email: Email.Create(value: $"{customerId:N}@example.test").Value,
            name: PersonName.Create(firstName: "Ada", lastName: "Lovelace").Value,
            phoneNumber: null);

        Customer customer = provisioned.Value.Customer;

        if (withAddress)
        {
            customer.AddAddress(
                street: "1 Rue de Carthage",
                city: "Tunis",
                postalCode: "1000",
                country: "TN");
        }

        await UnitOfWork.SaveChangesAsync();

        return customer;
    }

    /// <summary>
    /// Adds a product to a customer's basket through the real Basket command handler, so the document
    /// the order is later built from is the one production would have written to Redis.
    /// </summary>
    /// <param name="customerId">The customer whose basket to add to.</param>
    /// <param name="productId">The product to add.</param>
    /// <param name="quantity">The number of units to add.</param>
    protected async Task SeedBasketItemAsync(Guid customerId, Guid productId, int quantity)
    {
        Result<BasketDto> added = await Scope.ServiceProvider
            .GetRequiredService<ICommandHandler<AddItemToBasketCommand, BasketDto>>()
            .HandleAsync(
                command: new AddItemToBasketCommand(
                    CustomerId: customerId,
                    ProductId: productId,
                    Quantity: quantity),
                cancellationToken: CancellationToken.None);

        if (added.IsError)
        {
            throw new InvalidOperationException(
                message: $"Seeding the basket failed: {added.FirstError.Code} — {added.FirstError.Description}.");
        }
    }

    /// <summary>
    /// Places an order through the real, fully decorated command pipeline.
    /// </summary>
    /// <remarks>
    /// <c>PlaceOrderCommandHandler</c> is <c>[Idempotent]</c>, so each call needs its own key —
    /// exactly as each HTTP request would carry its own. A test that wants to exercise a retry passes
    /// the same key twice deliberately.
    /// </remarks>
    /// <param name="customerId">The customer placing the order.</param>
    /// <param name="idempotencyKey">The key to place under, defaulted to a fresh one.</param>
    /// <returns>The new order's identifier, or the pipeline's error.</returns>
    protected ValueTask<Result<OrderId>> PlaceOrderAsync(Guid customerId, string? idempotencyKey = null)
    {
        IdempotencyKeyContext.Current = idempotencyKey ?? Guid.CreateVersion7().ToString();

        return Scope.ServiceProvider.GetRequiredService<ICommandHandler<PlaceOrderCommand, OrderId>>()
            .HandleAsync(
                command: new PlaceOrderCommand(CustomerId: customerId),
                cancellationToken: CancellationToken.None);
    }
}
