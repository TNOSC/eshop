// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using Tnosc.EShop.Server.Domain.Catalog.Brands;
using Tnosc.EShop.Server.Domain.Catalog.Categories;
using Tnosc.EShop.Server.Domain.Catalog.Products.Events;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// A sellable item in the catalogue. Owns every rule about its own price, stock level and lifecycle.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    /// <summary>
    /// The maximum number of characters a product name may contain.
    /// </summary>
    public const int NameMaxLength = 200;

    /// <summary>
    /// The maximum number of characters a product description may contain.
    /// </summary>
    public const int DescriptionMaxLength = 2000;

    private Product()
    {
        // EF.
    }

    /// <summary>
    /// Gets the product's catalogue-unique stock-keeping unit.
    /// </summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the product's display name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the product's optional long-form description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the product's current price.
    /// </summary>
    public Money Price { get; private set; } = null!;

    /// <summary>
    /// Gets the number of units currently on hand.
    /// </summary>
    public StockQuantity Stock { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the brand the product belongs to.
    /// </summary>
    public BrandId BrandId { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the category the product belongs to.
    /// </summary>
    public CategoryId CategoryId { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the product has been withdrawn from sale.
    /// </summary>
    public bool IsDiscontinued { get; private set; }

    /// <summary>
    /// The maximum number of characters an image URL may contain.
    /// </summary>
    public const int ImageUrlMaxLength = 2048;

    /// <summary>
    /// Gets the URL of the product's image, or <see langword="null"/> when none has been uploaded.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Creates a product and raises <see cref="ProductCreatedDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately <see langword="internal"/>: SKU uniqueness spans the whole catalogue, so it cannot
    /// be decided here. <see cref="ProductFactory"/> is the only reachable way in from outside the
    /// domain, and it consults <see cref="IProductRepository"/> before delegating here — which is what
    /// keeps the uniqueness rule out of the command handler.
    /// </remarks>
    /// <param name="sku">The product's stock-keeping unit.</param>
    /// <param name="name">The product's display name.</param>
    /// <param name="description">The product's optional long-form description.</param>
    /// <param name="price">The product's initial price.</param>
    /// <param name="stock">The product's initial stock quantity.</param>
    /// <param name="brandId">The identifier of the brand the product belongs to.</param>
    /// <param name="categoryId">The identifier of the category the product belongs to.</param>
    /// <returns>
    /// The created product, or a <c>Product.NameRequired</c> / <c>Product.NameTooLong</c> /
    /// <c>Product.DescriptionTooLong</c> validation error.
    /// </returns>
    internal static Result<Product> Create(
        Sku sku,
        string? name,
        string? description,
        Money price,
        StockQuantity stock,
        BrandId brandId,
        CategoryId categoryId)
    {
        if (string.IsNullOrWhiteSpace(value: name))
        {
            return ProductErrors.NameRequired;
        }

        if (name.Length > NameMaxLength)
        {
            return ProductErrors.NameTooLong;
        }

        if (description is { Length: > DescriptionMaxLength })
        {
            return ProductErrors.DescriptionTooLong;
        }

        var product = new Product
        {
            Id = ProductId.New(),
            Sku = sku,
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            BrandId = brandId,
            CategoryId = categoryId,
            IsDiscontinued = false,
        };

        product.IncrementVersion();
        product.AddDomainEvent(domainEvent: new ProductCreatedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            ProductId: product.Id.Value,
            Sku: product.Sku.Value,
            Name: product.Name,
            PriceAmount: product.Price.Amount,
            PriceCurrency: product.Price.Currency,
            StockQuantity: product.Stock.Value,
            BrandId: product.BrandId.Value,
            CategoryId: product.CategoryId.Value));

        return product;
    }

    /// <summary>
    /// Reprices the product and raises <see cref="ProductPriceChangedDomainEvent"/>.
    /// </summary>
    /// <param name="newPrice">The product's new price.</param>
    /// <returns>Success, or a <c>Product.Discontinued</c> conflict when the product is no longer sold.</returns>
    public Result ChangePrice(Money newPrice)
    {
        if (IsDiscontinued)
        {
            return ProductErrors.Discontinued(productId: Id.Value);
        }

        Money oldPrice = Price;
        Price = newPrice;
        IncrementVersion();

        AddDomainEvent(domainEvent: new ProductPriceChangedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            ProductId: Id.Value,
            OldAmount: oldPrice.Amount,
            OldCurrency: oldPrice.Currency,
            NewAmount: newPrice.Amount,
            NewCurrency: newPrice.Currency));

        return Result.Success();
    }

    /// <summary>
    /// Applies a signed delta to the stock level and raises <see cref="ProductStockAdjustedDomainEvent"/>.
    /// </summary>
    /// <param name="delta">The signed number of units to add or remove.</param>
    /// <returns>Success, or the <c>StockQuantity.Negative</c> validation error when the result would go below zero.</returns>
    public Result AdjustStock(int delta)
    {
        Result<StockQuantity> adjusted = Stock.Adjust(delta: delta);

        if (adjusted.IsError)
        {
            return adjusted.Errors.ToArray();
        }

        Stock = adjusted.Value;
        IncrementVersion();

        AddDomainEvent(domainEvent: new ProductStockAdjustedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            ProductId: Id.Value,
            Delta: delta,
            NewQuantity: Stock.Value));

        return Result.Success();
    }

    /// <summary>
    /// Withdraws the product from sale.
    /// </summary>
    /// <returns>Success, or a <c>Product.AlreadyDiscontinued</c> conflict when it is already withdrawn.</returns>
    public Result Discontinue()
    {
        if (IsDiscontinued)
        {
            return ProductErrors.AlreadyDiscontinued(productId: Id.Value);
        }

        IsDiscontinued = true;
        IncrementVersion();

        return Result.Success();
    }


    /// <summary>
    /// Sets the product's image URL, replacing any previous one.
    /// </summary>
    /// <param name="imageUrl">The uploaded image's URL.</param>
    /// <returns>Success, or a <c>Product.ImageUrlRequired</c> / <c>Product.ImageUrlTooLong</c> validation error.</returns>
#pragma warning disable CA1054 // ImageUrl is a flat wire-format string throughout this codebase, like every other DTO field — never System.Uri.
    public Result SetImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(value: imageUrl))
        {
            return ProductErrors.ImageUrlRequired;
        }

        if (imageUrl.Length > ImageUrlMaxLength)
        {
            return ProductErrors.ImageUrlTooLong;
        }

        ImageUrl = imageUrl;
        IncrementVersion();

        return Result.Success();
    }
#pragma warning restore CA1054

    /// <summary>
    /// Clears the product's image URL.
    /// </summary>
    /// <returns>Always succeeds.</returns>
    public Result ClearImage()
    {
        ImageUrl = null;
        IncrementVersion();

        return Result.Success();
    }
}
