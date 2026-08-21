// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <inheritdoc />
    public async Task<Guid> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage httpRequest = new(method: HttpMethod.Post, requestUri: "/api/catalog/products")
        {
            Content = JsonContent.Create(inputValue: request, options: SerializerOptions),
        };
        httpRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using HttpResponseMessage response = await httpClient.SendAsync(
            request: httpRequest,
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string detail = await ReadProblemDetailAsync(response: response, cancellationToken: cancellationToken);

            throw new HttpRequestException(
                message: detail,
                inner: null,
                statusCode: response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<Guid>(
            options: SerializerOptions,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reads the RFC 9457 problem body a failed eShop API call returns and turns it into a single
    /// human-readable message — the field-level validation errors when present, else the problem's
    /// detail or title, else a fallback built from the status line.
    /// </summary>
    private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string fallback = $"{(int)response.StatusCode} {response.ReasonPhrase}";

        try
        {
            ProblemBody? problem = await response.Content.ReadFromJsonAsync<ProblemBody>(
                options: SerializerOptions,
                cancellationToken: cancellationToken);

            if (problem?.Errors is { Count: > 0 })
            {
                return string.Join(
                    separator: "; ",
                    values: problem.Errors.SelectMany(static kv => kv.Value.Select(message => $"{kv.Key}: {message}")));
            }

            return problem?.Detail ?? problem?.Title ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private sealed record ProductsPage(IReadOnlyCollection<Product> Items, int Page, int PageSize, long TotalCount);

    private sealed record ProblemBody(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
