// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Catalog.Queries.GetProductById;

/// <summary>
/// Reads a single product by its identifier.
/// </summary>
/// <param name="ProductId">The identifier of the product to read.</param>
public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;
