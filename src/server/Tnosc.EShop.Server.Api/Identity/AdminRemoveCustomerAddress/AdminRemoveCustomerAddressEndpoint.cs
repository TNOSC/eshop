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
using Tnosc.EShop.Server.Application.Identity.Commands.AdminRemoveCustomerAddress;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.AdminRemoveCustomerAddress;

/// <summary>
/// <c>DELETE /api/identity/customers/{id}/addresses/{addressId}</c> — removes one of a customer's
/// addresses.
/// </summary>
/// <remarks>
/// Requires the <c>identity:write</c> permission; a customer removing their own address calls
/// <c>DELETE /api/identity/customers/me/addresses/{addressId}</c> instead.
/// </remarks>
internal sealed class AdminRemoveCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapDelete(pattern: IdentityRoutes.CustomerAddressByIdAndAddressId, handler: HandleAsync)
           .WithName(endpointName: "AdminRemoveCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Remove one of a customer's addresses")
           .WithDescription(
               description: "Removes an address from a customer's profile. The default address cannot " +
                             "be removed and returns 409 — make another address the default first. " +
                             "Requires the 'identity:write' permission, which the 'admin' realm role grants.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .HasPermission(permission: Permissions.Identity.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid addressId,
        ICommandHandler<AdminRemoveCustomerAddressCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new AdminRemoveCustomerAddressCommand(
                CustomerId: id,
                AddressId: addressId),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
