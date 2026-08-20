// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tnosc.EShop.Server.Application.Identity.Queries.ListCustomers;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.ListCustomers;

/// <summary>
/// <c>GET /api/identity/customers</c> — lists customers, one page at a time.
/// </summary>
/// <remarks>
/// Requires the <c>identity:read</c> permission, same as reading a single customer by identifier.
/// </remarks>
internal sealed class ListCustomersEndpoint : IApiEndpoint
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: IdentityRoutes.Customers, handler: HandleAsync)
           .WithName(endpointName: "ListCustomers")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "List customers")
           .WithDescription(
               description: "Lists customers by free-text term and/or active status, returning one " +
                             "page of customer summaries at a time. Requires the 'identity:read' " +
                             "permission, which the 'admin' realm role grants.")
           .Produces<PagedResult<CustomerSummaryDto>>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .HasPermission(permission: Permissions.Identity.Read);

    private static async Task<IResult> HandleAsync(
        IQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryDto>> handler,
        CancellationToken cancellationToken,
        string? search = null,
        bool? isActive = null,
        int page = DefaultPage,
        int pageSize = DefaultPageSize)
    {
        Result<PagedResult<CustomerSummaryDto>> result = await handler.HandleAsync(
            query: new ListCustomersQuery(
                SearchTerm: search,
                IsActive: isActive,
                Page: page,
                PageSize: pageSize),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static customers => Results.Ok(value: customers));
    }
}
