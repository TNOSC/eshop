// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Mcp.Application.Products;

namespace Tnosc.EShop.Mcp.Application.Extensions;

/// <summary>
/// Composition root for <c>Mcp.Application</c> — the orchestration services a Tool delegates to.
/// </summary>
public static class McpApplicationExtensions
{
    /// <summary>
    /// Registers the Application layer's services: <see cref="IProductsQueryService"/> and
    /// <see cref="IProductsCommandService"/>.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddMcpApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductsQueryService, ProductsQueryService>();
        services.AddScoped<IProductsCommandService, ProductsCommandService>();

        return services;
    }
}
