// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Tnosc.Lib.Host.Authorization;

/// <summary>
/// Succeeds a <see cref="PermissionRequirement"/> when the caller carries the matching permission claim.
/// </summary>
/// <remarks>
/// The permission is read straight off the <c>ClaimsPrincipal</c> the authorization pipeline hands in,
/// deliberately <strong>not</strong> through <c>IUserContext</c>. Authorization handlers are resolved as
/// singletons, while <c>IUserContext</c> is scoped; injecting it here would be a captive-dependency
/// lifetime mismatch. The principal on the requirement's context is the same one <c>IUserContext</c>
/// would have read anyway.
/// </remarks>
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
