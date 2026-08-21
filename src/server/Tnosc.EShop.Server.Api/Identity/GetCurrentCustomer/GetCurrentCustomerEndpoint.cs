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
using Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.GetCurrentCustomer;

/// <summary>
/// <c>GET /api/identity/customers/me</c> — reads the caller's own customer profile.
/// </summary>
internal sealed class GetCurrentCustomerEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: IdentityRoutes.CurrentCustomer, handler: HandleAsync)
           .WithName(endpointName: "GetCurrentCustomer")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Get the caller's customer profile")
           .WithDescription(
               description: "Reads the profile belonging to the caller's access token, including their " +
                             "addresses and which of them is the default. Returns 404 when no profile " +
                             "has been provisioned yet — POST to /api/identity/customers first.")
           .Produces<CustomerDto>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        IUserContext userContext,
        IQueryHandler<GetCurrentCustomerQuery, CustomerDto> handler,
        CancellationToken cancellationToken)
    {
        Result<CustomerDto> result = await handler.HandleAsync(
            query: new GetCurrentCustomerQuery(ExternalUserId: userContext.UserId),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static customer => Results.Ok(value: customer));
    }
}
