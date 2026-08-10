// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Catalog.Queries.SearchProducts;

/// <summary>
/// Searches the catalogue, optionally narrowing by free text and category, one page at a time.
/// </summary>
/// <param name="SearchTerm">Free text matched against the product name and SKU, or <see langword="null"/> for no text filter.</param>
/// <param name="CategoryId">The category to restrict results to, or <see langword="null"/> for every category.</param>
/// <param name="Page">The one-based page number to read.</param>
/// <param name="PageSize">The maximum number of products to return.</param>
public sealed record SearchProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    int Page,
    int PageSize) : IQuery<PagedResult<ProductSummaryDto>>;
