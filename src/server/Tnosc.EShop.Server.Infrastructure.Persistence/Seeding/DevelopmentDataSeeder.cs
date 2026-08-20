// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Seeding;

/// <summary>
/// Writes <see cref="SeedData"/> into an empty database so a fresh clone comes up with something to
/// browse, buy and pay for.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Two gates, and both must be open.</strong> Registration happens only when the host runs in
/// the Development environment, and <see cref="SeedOptions.Enabled"/> must also be
/// <see langword="true"/>. Neither is redundant: the environment check is what makes it structurally
/// impossible for sample data to reach Production, and the flag is what lets a developer run
/// Development against a database they have curated by hand.
/// </para>
/// <para>
/// <strong>Idempotent by natural key, not by a marker row.</strong> Every aggregate is looked up by
/// the thing that identifies it to a human — brand and category by name, product by SKU, customer by
/// external id — and skipped when it is already there. So a run against a half-seeded database
/// completes the set rather than duplicating it or giving up, and running on every startup is safe.
/// </para>
/// <para>
/// <strong>Everything goes through a domain factory.</strong> <see cref="ProductFactory"/> and
/// <see cref="Customer.Register"/> are the same entry points the command handlers use, and the write
/// goes through <see cref="IUnitOfWork"/> — so seeded aggregates are audited and their creation events
/// reach the outbox exactly as a real write's would. Inserting rows straight into the tables would be
/// shorter and would quietly bypass every invariant this codebase exists to enforce.
/// </para>
/// <para>
/// It implements <see cref="IHostedService"/> rather than <see cref="BackgroundService"/> so seeding
/// finishes before the host serves traffic, and it is registered after
/// <c>AddPersistence</c> so the migration hosted service has already run.
/// </para>
/// </remarks>
/// <param name="scopeFactory">Creates the scope the seeding services are resolved from.</param>
/// <param name="options">The seeding switch, bound from the <c>"Seed"</c> configuration section.</param>
/// <param name="logger">Records what was seeded, or why nothing was.</param>
internal sealed class DevelopmentDataSeeder(
    IServiceScopeFactory scopeFactory,
    SeedOptions options,
    ILogger<DevelopmentDataSeeder> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation(message: "Development seeding is switched off; no sample data was written.");
            return;
        }

        // CreateAsyncScope, not CreateScope: UnitOfWork implements only IAsyncDisposable, and
        // IServiceScope.Dispose() throws for such a service. See .claude/rules/idempotency.md.
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        EShopWriteDbContext context = scope.ServiceProvider.GetRequiredService<EShopWriteDbContext>();
        IProductRepository products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        ICustomerRepository customers = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyDictionary<string, BrandId> brandIds = await SeedBrandsAsync(context: context, cancellationToken: cancellationToken);
        IReadOnlyDictionary<string, CategoryId> categoryIds = await SeedCategoriesAsync(context: context, cancellationToken: cancellationToken);

        // Brands and categories are committed first because the products that follow carry foreign
        // keys onto them, and the seeded product rows are written in the same call as their parents
        // only if EF happens to order them correctly — which is not something to rely on.
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        await SeedProductsAsync(
            products: products,
            brandIds: brandIds,
            categoryIds: categoryIds,
            cancellationToken: cancellationToken);

        await SeedDemoCustomerAsync(customers: customers, cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        logger.LogInformation(
            message: "Development seeding completed: {BrandCount} brands, {CategoryCount} categories and {ProductCount} products are available.",
            args: [SeedData.Brands.Count, SeedData.Categories.Count, SeedData.Products.Count]);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<IReadOnlyDictionary<string, BrandId>> SeedBrandsAsync(
        EShopWriteDbContext context,
        CancellationToken cancellationToken)
    {
        Dictionary<string, BrandId> brandIds = new(comparer: StringComparer.Ordinal);

        foreach (string name in SeedData.Brands)
        {
            Brand? existing = await context.Set<Brand>()
                .FirstOrDefaultAsync(predicate: brand => brand.Name == name, cancellationToken: cancellationToken);

            Brand brand = existing ?? Require(result: Brand.Create(name: name), what: $"brand '{name}'");

            if (existing is null)
            {
                await context.Set<Brand>().AddAsync(entity: brand, cancellationToken: cancellationToken);
            }

            brandIds[name] = brand.Id;
        }

        return brandIds;
    }

    private static async Task<IReadOnlyDictionary<string, CategoryId>> SeedCategoriesAsync(
        EShopWriteDbContext context,
        CancellationToken cancellationToken)
    {
        Dictionary<string, CategoryId> categoryIds = new(comparer: StringComparer.Ordinal);

        foreach (string name in SeedData.Categories)
        {
            Category? existing = await context.Set<Category>()
                .FirstOrDefaultAsync(predicate: category => category.Name == name, cancellationToken: cancellationToken);

            Category category = existing ?? Require(result: Category.Create(name: name), what: $"category '{name}'");

            if (existing is null)
            {
                await context.Set<Category>().AddAsync(entity: category, cancellationToken: cancellationToken);
            }

            categoryIds[name] = category.Id;
        }

        return categoryIds;
    }

    private static async Task SeedProductsAsync(
        IProductRepository products,
        IReadOnlyDictionary<string, BrandId> brandIds,
        IReadOnlyDictionary<string, CategoryId> categoryIds,
        CancellationToken cancellationToken)
    {
        foreach (SeedProduct specification in SeedData.Products)
        {
            Sku sku = Require(result: Sku.Create(value: specification.Sku), what: $"SKU '{specification.Sku}'");

            Product? existing = await products.GetBySkuAsync(sku: sku, cancellationToken: cancellationToken);

            if (existing is not null)
            {
                continue;
            }

            Money price = Require(
                result: Money.Create(amount: specification.PriceAmount, currency: SeedData.Currency),
                what: $"price of '{specification.Sku}'");

            StockQuantity stock = Require(
                result: StockQuantity.Create(value: specification.Stock),
                what: $"stock of '{specification.Sku}'");

            Product product = Require(
                result: await ProductFactory.CreateAsync(
                    repository: products,
                    sku: sku,
                    name: specification.Name,
                    description: specification.Description,
                    price: price,
                    stock: stock,
                    brandId: brandIds[specification.BrandName],
                    categoryId: categoryIds[specification.CategoryName],
                    cancellationToken: cancellationToken),
                what: $"product '{specification.Sku}'");

            await products.AddAsync(aggregate: product, cancellationToken: cancellationToken);
        }
    }

    private static async Task SeedDemoCustomerAsync(ICustomerRepository customers, CancellationToken cancellationToken)
    {
        ExternalUserId externalUserId = Require(
            result: ExternalUserId.Create(value: SeedData.DemoCustomerExternalUserId),
            what: "demo customer external id");

        // ProvisionAsync rather than a lookup plus Customer.Register: registering is internal to the
        // domain, and the factory is the same door the provisioning endpoint comes through. It also
        // carries the "already there?" decision and the AddAsync, so this method never repeats either.
        CustomerProvisioning provisioned = Require(
            result: await CustomerFactory.ProvisionAsync(
                repository: customers,
                externalUserId: externalUserId,
                email: Require(result: Email.Create(value: SeedData.DemoCustomerEmail), what: "demo customer email"),
                name: Require(result: PersonName.Create(firstName: "Demo", lastName: "Customer"), what: "demo customer name"),
                phoneNumber: Require(result: PhoneNumber.Create(value: "+21671000000"), what: "demo customer phone number"),
                cancellationToken: cancellationToken),
            what: "demo customer");

        // Only a customer this run created needs an address; adding one on every startup would grow a
        // new address per restart, and the first one added is already their default.
        if (!provisioned.WasCreated)
        {
            return;
        }

        Require(
            result: provisioned.Customer.AddAddress(
                street: "12 Avenue Habib Bourguiba",
                city: "Tunis",
                postalCode: "1001",
                country: "TN"),
            what: "demo customer address");
    }

    // Seed data is written by a developer, not by a caller, so a domain rule rejecting it is a defect
    // in this file rather than a runtime condition to handle. Failing the host's startup makes that
    // obvious immediately; carrying on would leave a half-seeded database and a puzzling 404 later.
    private static T Require<T>(Result<T> result, string what) =>
        result.IsError
            ? throw new InvalidOperationException(
                message: $"Seeding {what} failed: {result.FirstError.Code} — {result.FirstError.Description}.")
            : result.Value;
}
