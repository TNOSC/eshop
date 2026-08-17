// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

/// <summary>
/// Succeeds a <see cref="PermissionRequirement"/> when the caller carries the matching permission claim.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(argument: context);
        ArgumentNullException.ThrowIfNull(argument: requirement);

        bool hasPermission = context.User.HasClaim(
            type: PermissionRequirement.PermissionClaimType,
            value: requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement: requirement);
        }

        // Deliberately no Fail() call: leaving the requirement merely unmet lets another handler for
        // the same requirement succeed it, whereas Fail() is final and cannot be overridden.
        return Task.CompletedTask;
    }
}
