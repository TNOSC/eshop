// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

namespace Tnosc.EShop.Client.Web.Bff;

/// <summary>Returns the authenticated caller's identity, realm roles and permissions.</summary>
internal static class UserInfoEndpoint
{
    /// <summary>Maps <see cref="BffRoutes.UserInfo"/>.</summary>
    /// <param name="app">The application to map the route on.</param>
    public static void MapUserInfo(WebApplication app) =>
        app.MapGet(pattern: BffRoutes.UserInfo, handler: Handle).RequireAuthorization();

    private static UserInfo Handle(ClaimsPrincipal user) =>
        new(
            UserId: user.FindFirst(type: ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            Name: user.Identity?.Name ?? string.Empty,
            Roles: [.. user.FindAll(type: ClaimTypes.Role).Select(selector: static claim => claim.Value)],
            Permissions:
            [
                .. user.FindAll(type: PermissionRequirement.PermissionClaimType)
                    .Select(selector: static claim => claim.Value),
            ]);
}
