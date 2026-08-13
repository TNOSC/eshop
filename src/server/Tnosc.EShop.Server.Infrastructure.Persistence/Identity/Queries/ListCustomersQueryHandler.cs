// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Identity.Queries.ListCustomers;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Identity.ReadModels;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Identity.Queries;

/// <summary>
/// Lists customers with LINQ over a single table — no join, so the raw-SQL convention this project
/// otherwise follows for query handlers does not apply here.
/// </summary>
/// <param name="context">The read context.</param>
internal sealed class ListCustomersQueryHandler(EShopReadDbContext context)
    : IQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryDto>>
{
    /// <summary>
    /// The largest page this query will serve, however large a page the caller asks for.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <inheritdoc />
    public async ValueTask<Result<PagedResult<CustomerSummaryDto>>> HandleAsync(
        ListCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = Math.Max(val1: query.Page, val2: 1);
        int pageSize = Math.Clamp(value: query.PageSize, min: 1, max: MaxPageSize);

        IQueryable<CustomerReadModel> filtered = context.Set<CustomerReadModel>()
            .Where(predicate: customer =>
                (query.SearchTerm == null ||
                 EF.Functions.ILike(matchExpression: customer.Email, pattern: $"%{query.SearchTerm}%") ||
                 EF.Functions.ILike(matchExpression: customer.FirstName, pattern: $"%{query.SearchTerm}%") ||
                 EF.Functions.ILike(matchExpression: customer.LastName, pattern: $"%{query.SearchTerm}%")) &&
                (query.IsActive == null || customer.IsActive == query.IsActive));

        long totalCount = await filtered.LongCountAsync(cancellationToken: cancellationToken);

        List<CustomerSummaryDto> items = await filtered
            .OrderBy(keySelector: static customer => customer.Email)
            .ThenBy(keySelector: static customer => customer.Id)
            .Skip(count: (page - 1) * pageSize)
            .Take(count: pageSize)
            .Select(selector: static customer => new CustomerSummaryDto(
                Id: customer.Id,
                Email: customer.Email,
                FirstName: customer.FirstName,
                LastName: customer.LastName,
                IsActive: customer.IsActive))
            .ToListAsync(cancellationToken: cancellationToken);

        return new PagedResult<CustomerSummaryDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount);
    }
}
