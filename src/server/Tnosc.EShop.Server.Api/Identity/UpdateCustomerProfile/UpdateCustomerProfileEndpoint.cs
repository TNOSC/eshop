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
using Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerProfile;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.UpdateCustomerProfile;

/// <summary>
/// <c>PUT /api/identity/customers/me/profile</c> — updates the caller's name and phone number.
/// </summary>
internal sealed class UpdateCustomerProfileEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: IdentityRoutes.CurrentCustomerProfile, handler: HandleAsync)
           .WithName(endpointName: "UpdateCustomerProfile")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Update the caller's name and phone number")
           .WithDescription(
               description: "Updates the parts of the profile this API owns: the caller's name and " +
                             "phone number. Email address and password are deliberately NOT editable " +
                             "here — Keycloak owns both, and a customer changes them (and resets a " +
                             "forgotten password) in its Account Console at " +
                             $"{IdentityRoutes.AccountConsolePath}. The local email copy is reconciled " +
                             "from the access token on the next login.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        UpdateCustomerProfileRequest request,
        IUserContext userContext,
        ICommandHandler<UpdateCustomerProfileCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: request.ToCommand(userContext: userContext),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
