// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Mcp.Application.Products;

/// <summary>
/// Mutates products for MCP tools to expose. Owns the orchestration of the call to the eShop
/// catalog; a tool only validates its own parameters and delegates here.
/// </summary>
public interface IProductsCommandService
{
    /// <summary>
    /// Adds a new product to the eShop catalog.
    /// </summary>
    /// <param name="request">The data describing the product to create.</param>
    /// <param name="cancellationToken">The token used to cancel the request.</param>
    /// <returns>
    /// A successful result carrying the new product's identifier, or a failed result describing
    /// why the eShop catalog rejected or could not process the request.
    /// </returns>
    Task<Result<Guid>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);
}
