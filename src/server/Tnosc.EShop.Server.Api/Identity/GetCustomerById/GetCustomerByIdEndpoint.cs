// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;
using Tnosc.EShop.Server.Application.Identity.Queries.GetCustomerById;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.GetCustomerById;

/// <summary>
/// <c>GET /api/identity/customers/{id}</c> — reads any customer's profile.
/// </summary>
/// <remarks>
/// The only Identity endpoint that names a customer other than the caller, and therefore the only one
/// carrying a permission: everything else resolves the caller from their own token.
/// </remarks>
internal sealed class GetCustomerByIdEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: IdentityRoutes.CustomerById, handler: HandleAsync)
           .WithName(endpointName: "GetCustomerById")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Get a customer by identifier")
           .WithDescription(
               description: "Reads any customer's profile by identifier. Requires the 'identity:read' " +
                             "permission, which the 'admin' realm role grants; a customer reading their " +
                             "own profile should call /api/identity/customers/me instead.")
           .Produces<CustomerDto>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Identity.Read);

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetCustomerByIdQuery, CustomerDto> handler,
        CancellationToken cancellationToken)
    {
        Result<CustomerDto> result = await handler.HandleAsync(
            query: new GetCustomerByIdQuery(CustomerId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static customer => Results.Ok(value: customer));
    }
}
