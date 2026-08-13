// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tnosc.EShop.Server.Host.OpenApi;

/// <summary>
/// Marks every operation whose endpoint carries <see cref="IAuthorizeData"/> — i.e. every
/// <c>.HasPermission(...)</c> or <c>.RequireAuthorization()</c> endpoint — as requiring the
/// <see cref="KeycloakOAuthSecuritySchemeTransformer.SecuritySchemeName"/> scheme, so Scalar shows a
/// lock icon on it and attaches the bearer token once the user has authorized.
/// </summary>
internal sealed class AuthorizedOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        bool requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (requiresAuthorization)
        {
            operation.Security ??= [];
            operation.Security.Add(item: new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(referenceId: KeycloakOAuthSecuritySchemeTransformer.SecuritySchemeName, hostDocument: context.Document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
