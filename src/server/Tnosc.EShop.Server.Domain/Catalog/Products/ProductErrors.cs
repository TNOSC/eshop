// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// Every failure a caller can get back about a <see cref="Product"/>, defined once.
/// </summary>
/// <remarks>
/// The code, the wording and — crucially — the <c>ErrorType</c> that decides the HTTP status live
/// here rather than at each call site, so the aggregate, the command handlers, the validators and the
/// query handlers cannot drift apart on what "not found" means or which status it maps to.
/// </remarks>
public static class ProductErrors
{
    /// <summary>
    /// No product carries the requested identifier.
    /// </summary>
    /// <param name="productId">The identifier that was looked up.</param>
    public static Error NotFound(Guid productId) => Error.NotFound(
        code: "Product.NotFound",
        description: $"Product {productId} was not found.");

    /// <summary>
    /// Another product in the catalogue already holds the requested SKU.
    /// </summary>
    /// <param name="sku">The SKU that is already taken.</param>
    public static Error SkuAlreadyExists(string sku) => Error.Conflict(
        code: "Product.SkuAlreadyExists",
        description: $"A product with SKU '{sku}' already exists.");

    /// <summary>
    /// The product has been withdrawn from sale and cannot be repriced.
    /// </summary>
    /// <param name="productId">The identifier of the discontinued product.</param>
    public static Error Discontinued(Guid productId) => Error.Conflict(
        code: "Product.Discontinued",
        description: $"Product {productId} is discontinued and cannot be repriced.");

    /// <summary>
    /// The product has already been withdrawn from sale.
    /// </summary>
    /// <param name="productId">The identifier of the discontinued product.</param>
    public static Error AlreadyDiscontinued(Guid productId) => Error.Conflict(
        code: "Product.AlreadyDiscontinued",
        description: $"Product {productId} is already discontinued.");

    /// <summary>
    /// Gets the error returned when a product name is missing.
    /// </summary>
    public static Error NameRequired => Error.Validation(
        code: "Product.NameRequired",
        description: "A product name is required.");

    /// <summary>
    /// Gets the error returned when a product name exceeds <see cref="Product.NameMaxLength"/>.
    /// </summary>
    public static Error NameTooLong => Error.Validation(
        code: "Product.NameTooLong",
        description: $"A product name must be at most {Product.NameMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when a product description exceeds <see cref="Product.DescriptionMaxLength"/>.
    /// </summary>
    public static Error DescriptionTooLong => Error.Validation(
        code: "Product.DescriptionTooLong",
        description: $"A product description must be at most {Product.DescriptionMaxLength} characters long.");

    /// <summary>
    /// Gets the error returned when a product identifier is missing.
    /// </summary>
    public static Error IdRequired => Error.Validation(
        code: "Product.IdRequired",
        description: "A product identifier is required.");

    /// <summary>
    /// Gets the error returned when a brand identifier is missing.
    /// </summary>
    public static Error BrandRequired => Error.Validation(
        code: "Product.BrandRequired",
        description: "A brand identifier is required.");

    /// <summary>
    /// Gets the error returned when a category identifier is missing.
    /// </summary>
    public static Error CategoryRequired => Error.Validation(
        code: "Product.CategoryRequired",
        description: "A category identifier is required.");

    /// <summary>
    /// Gets the error returned when a stock adjustment would not move the stock level at all.
    /// </summary>
    public static Error StockDeltaRequired => Error.Validation(
        code: "Product.StockDeltaRequired",
        description: "A stock adjustment must move the stock level by a non-zero amount.");
}
