// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Identity.Queries.ListCustomers;

/// <summary>
/// Lists customers, optionally narrowing by free text and active status, one page at a time.
/// </summary>
/// <param name="SearchTerm">Free text matched against the customer's email and name, or <see langword="null"/> for no text filter.</param>
/// <param name="IsActive">Restricts to active or deactivated customers, or <see langword="null"/> for both.</param>
/// <param name="Page">The one-based page number to read.</param>
/// <param name="PageSize">The maximum number of customers to return.</param>
public sealed record ListCustomersQuery(
    string? SearchTerm,
    bool? IsActive,
    int Page,
    int PageSize) : IQuery<PagedResult<CustomerSummaryDto>>;
