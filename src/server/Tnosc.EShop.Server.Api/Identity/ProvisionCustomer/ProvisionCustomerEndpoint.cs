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
using Tnosc.EShop.Server.Application.Identity.Commands.ProvisionCustomer;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Identity.ProvisionCustomer;

/// <summary>
/// <c>POST /api/identity/customers</c> — provisions the caller's local customer profile.
/// </summary>
/// <remarks>
/// The call a client makes after <strong>every</strong> login, not only the first: <c>201</c> the
/// first time, <c>200</c> on later logins with the email reconciled from the token, and <c>409</c>
/// when that email already belongs to a different identity-provider account. There is no anonymous
/// sign-up endpoint here at all — registration is Keycloak's hosted page.
/// </remarks>
internal sealed class ProvisionCustomerEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: IdentityRoutes.Customers, handler: HandleAsync)
           .WithName(endpointName: "ProvisionCustomer")
           .WithTags(IdentityRoutes.Tag)
           .WithSummary(summary: "Provision the caller's customer profile")
           .WithDescription(
               description: "Creates the caller's local customer profile on first login and returns 201; " +
                             "on every later login it returns 200 with the email reconciled from the " +
                             "access token. The subject and email are taken from the token, never from " +
                             "the body, so a caller cannot provision a profile for anyone else. Sign-up " +
                             "itself happens on Keycloak's hosted registration page, not here.")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .Produces<Guid>(statusCode: StatusCodes.Status200OK)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        ProvisionCustomerRequest request,
        IUserContext userContext,
        ICommandHandler<ProvisionCustomerCommand, ProvisionCustomerResult> handler,
        CancellationToken cancellationToken)
    {
        Result<ProvisionCustomerResult> result = await handler.HandleAsync(
            command: request.ToCommand(userContext: userContext),
            cancellationToken: cancellationToken);

        // 201 versus 200 is not a decision made here — the domain already decided whether this call
        // registered the customer, and WasCreated carries that verdict out. This only maps it to HTTP.
        return result.ToHttp(onSuccess: static provisioned => provisioned.WasCreated
            ? Results.Created(uri: $"{IdentityRoutes.Customers}/{provisioned.CustomerId}", value: provisioned.CustomerId)
            : Results.Ok(value: provisioned.CustomerId));
    }
}
