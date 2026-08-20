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
using Tnosc.EShop.Server.Application.Identity.Commands.AdminAddCustomerAddress;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.AdminAddCustomerAddress;

/// <summary>
/// <c>POST /api/identity/customers/{id}/addresses</c> — adds an address to a customer's profile.
/// </summary>
/// <remarks>
/// Requires the <c>identity:write</c> permission; a customer adding to their own profile calls
/// <c>POST /api/identity/customers/me/addresses</c> instead.
/// </remarks>
internal sealed class AdminAddCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: IdentityRoutes.CustomerAddressesById, handler: HandleAsync)
           .WithName(endpointName: "AdminAddCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Add an address to a customer's profile")
           .WithDescription(
               description: "Adds an address to a customer's profile. The first address a customer " +
                             "holds automatically becomes their default. Requires the 'identity:write' " +
                             "permission, which the 'admin' realm role grants.")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Identity.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        AdminAddCustomerAddressRequest request,
        ICommandHandler<AdminAddCustomerAddressCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await handler.HandleAsync(
            command: request.ToCommand(customerId: id),
            cancellationToken: cancellationToken);

        return result.ToCreated(location: addressId =>
            $"{IdentityRoutes.Customers}/{id}/addresses/{addressId}");
    }
}
