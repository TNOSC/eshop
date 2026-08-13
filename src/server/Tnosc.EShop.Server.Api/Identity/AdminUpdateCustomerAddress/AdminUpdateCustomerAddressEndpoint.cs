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
using Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerAddress;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.AdminUpdateCustomerAddress;

/// <summary>
/// <c>PUT /api/identity/customers/{id}/addresses/{addressId}</c> — replaces one of a customer's
/// addresses.
/// </summary>
/// <remarks>
/// Requires the <c>identity:write</c> permission; a customer updating their own address calls
/// <c>PUT /api/identity/customers/me/addresses/{addressId}</c> instead.
/// </remarks>
internal sealed class AdminUpdateCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: IdentityRoutes.CustomerAddressByIdAndAddressId, handler: HandleAsync)
           .WithName(endpointName: "AdminUpdateCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Update one of a customer's addresses")
           .WithDescription(
               description: "Replaces the contents of one of a customer's addresses. Requires the " +
                             "'identity:write' permission, which the 'admin' realm role grants. An " +
                             "address belonging to a different customer is simply not found.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Identity.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid addressId,
        AdminUpdateCustomerAddressRequest request,
        ICommandHandler<AdminUpdateCustomerAddressCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: request.ToCommand(customerId: id, addressId: addressId),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
