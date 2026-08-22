// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Tnosc.EShop.Mcp.Application.Products;
using Tnosc.EShop.Mcp.Tool.Extensions;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Mcp.Tool.Products;

/// <summary>
/// Exposes the eShop catalog's products to MCP clients.
/// </summary>
[McpServerToolType]
public static class ProductsTool
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    /// <summary>Lists products from the eShop catalog, optionally filtered by a free-text search term.</summary>
    [McpServerTool(UseStructuredContent = true, Name = McpToolNames.ListProducts)]
    [Description("Lists products from the eShop catalog, optionally filtered by a free-text search term.")]
    public static async Task<ToolResult<IReadOnlyCollection<Product>>> ListProductsAsync(
        IProductsQueryService productsQueryService,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1)
        {
            return ToolResult<IReadOnlyCollection<Product>>.Fail(
                errorCode: "Products.InvalidPage",
                errorMessage: "page must be greater than or equal to 1.");
        }

        if (pageSize < MinPageSize || pageSize > MaxPageSize)
        {
            return ToolResult<IReadOnlyCollection<Product>>.Fail(
                errorCode: "Products.InvalidPageSize",
                errorMessage: $"pageSize must be between {MinPageSize} and {MaxPageSize}.");
        }

        Result<IReadOnlyCollection<Product>> result = await productsQueryService.GetProductsAsync(
            search: search,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        return result.ToToolResult();
    }

    /// <summary>Adds a new product to the eShop catalog. Requires the <c>catalog:write</c> permission.</summary>
    [McpServerTool(UseStructuredContent = true, Name = McpToolNames.CreateProduct)]
    [Description("Adds a new product to the eShop catalog. Returns the new product's identifier.")]
    [Authorize(Policy = Permissions.Catalog.Write)]
    public static async Task<ToolResult<Guid>> CreateProductAsync(
        IProductsCommandService productsCommandService,
        string sku,
        string name,
        decimal priceAmount,
        string priceCurrency,
        int stockQuantity,
        string brandId,
        string categoryId,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(input: brandId, result: out Guid parsedBrandId))
        {
            return ToolResult<Guid>.Fail(
                errorCode: "Products.InvalidBrandId",
                errorMessage: "brandId must be a valid GUID.");
        }

        if (!Guid.TryParse(input: categoryId, result: out Guid parsedCategoryId))
        {
            return ToolResult<Guid>.Fail(
                errorCode: "Products.InvalidCategoryId",
                errorMessage: "categoryId must be a valid GUID.");
        }

        Result<Guid> result = await productsCommandService.CreateProductAsync(
            request: new CreateProductRequest(
                Sku: sku,
                Name: name,
                Description: description,
                PriceAmount: priceAmount,
                PriceCurrency: priceCurrency,
                StockQuantity: stockQuantity,
                BrandId: parsedBrandId,
                CategoryId: parsedCategoryId),
            cancellationToken: cancellationToken);

        return result.ToToolResult();
    }
}
