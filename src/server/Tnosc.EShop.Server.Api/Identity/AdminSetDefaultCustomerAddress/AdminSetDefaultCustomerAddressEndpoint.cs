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
using Tnosc.EShop.Server.Application.Identity.Commands.AdminSetDefaultCustomerAddress;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.AdminSetDefaultCustomerAddress;

/// <summary>
/// <c>PUT /api/identity/customers/{id}/addresses/{addressId}/default</c> — makes one of a customer's
/// addresses their default.
/// </summary>
/// <remarks>
/// Requires the <c>identity:write</c> permission; a customer selecting their own default calls
/// <c>PUT /api/identity/customers/me/addresses/{addressId}/default</c> instead.
/// </remarks>
internal sealed class AdminSetDefaultCustomerAddressEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: IdentityRoutes.CustomerDefaultAddressById, handler: HandleAsync)
           .WithName(endpointName: "AdminSetDefaultCustomerAddress")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Make one of a customer's addresses their default")
           .WithDescription(
               description: "Selects which of a customer's addresses is their default. Requires the " +
                             "'identity:write' permission, which the 'admin' realm role grants.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Identity.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid addressId,
        ICommandHandler<AdminSetDefaultCustomerAddressCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new AdminSetDefaultCustomerAddressCommand(
                CustomerId: id,
                AddressId: addressId),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
