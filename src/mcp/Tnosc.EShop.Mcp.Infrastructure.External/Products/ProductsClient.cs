// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Mcp.Application.Products;
using Tnosc.EShop.Mcp.Application.Products.Ports;

namespace Tnosc.EShop.Mcp.Infrastructure.External.Products;

/// <summary>
/// Calls the eShop API's Catalog search endpoint over a typed <see cref="HttpClient"/>.
/// </summary>
/// <param name="httpClient">
/// The typed client registered by <see cref="Extensions.McpInfrastructureExtensions.AddMcpInfrastructureExternal"/>,
/// pointed at the eShop API host via Aspire service discovery.
/// </param>
internal sealed class ProductsClient(HttpClient httpClient) : IProductsClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        string requestUri = $"/api/catalog/products?page={page}&pageSize={pageSize}" +
            (string.IsNullOrWhiteSpace(value: search) ? string.Empty : $"&search={Uri.EscapeDataString(stringToEscape: search)}");

        ProductsPage? productsPage = await httpClient.GetFromJsonAsync<ProductsPage>(
            requestUri: requestUri,
            options: SerializerOptions,
            cancellationToken: cancellationToken);

        return productsPage?.Items ?? [];
    }

    private sealed record ProductsPage(IReadOnlyCollection<Product> Items, int Page, int PageSize, long TotalCount);
}
