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
using Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerProfile;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.AdminUpdateCustomerProfile;

/// <summary>
/// <c>PUT /api/identity/customers/{id}/profile</c> — updates a customer's name and phone number.
/// </summary>
/// <remarks>
/// Requires the <c>identity:write</c> permission; a customer updating their own profile calls
/// <c>PUT /api/identity/customers/me/profile</c> instead.
/// </remarks>
internal sealed class AdminUpdateCustomerProfileEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: IdentityRoutes.CustomerProfileById, handler: HandleAsync)
           .WithName(endpointName: "AdminUpdateCustomerProfile")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Update a customer's name and phone number")
           .WithDescription(
               description: "Updates the parts of the profile this API owns: the customer's name and " +
                             "phone number. Requires the 'identity:write' permission, which the 'admin' " +
                             "realm role grants. Email address and password are deliberately NOT " +
                             "editable here — Keycloak owns both.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .HasPermission(permission: Permissions.Identity.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        AdminUpdateCustomerProfileRequest request,
        ICommandHandler<AdminUpdateCustomerProfileCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: request.ToCommand(customerId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
