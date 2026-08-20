// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Tnosc.EShop.Mcp.Application.Products;
using Tnosc.EShop.Mcp.Tool.Extensions;
using Tnosc.Lib.Domain.Results;

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
    [McpServerTool(UseStructuredContent = true)]
    [Description("Lists products from the eShop catalog, optionally filtered by a free-text search term.")]
    public static async Task<IReadOnlyCollection<Product>> ListProductsAsync(
        IProductsQueryService productsQueryService,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1)
        {
            throw new McpException(message: "page must be greater than or equal to 1.");
        }

        if (pageSize < MinPageSize || pageSize > MaxPageSize)
        {
            throw new McpException(message: $"pageSize must be between {MinPageSize} and {MaxPageSize}.");
        }

        Result<IReadOnlyCollection<Product>> result = await productsQueryService.GetProductsAsync(
            search: search,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        return result.GetValueOrThrowMcpException();
    }
}
