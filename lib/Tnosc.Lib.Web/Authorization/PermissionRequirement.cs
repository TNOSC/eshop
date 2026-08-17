// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;

namespace Tnosc.Lib.Web.Authorization;

/// <summary>
/// An authorization requirement satisfied by the caller holding a named permission.
/// </summary>
/// <remarks>
/// Identity-provider agnostic by design: this half of the model knows only that permissions arrive as
/// claims of type <see cref="PermissionClaimType"/>. Which provider issued the token, and how its
/// roles or groups became those claims, is the host application's concern — see the eShop server's
/// <c>KeycloakClaimsTransformation</c> for one such mapping. The same type serves both an ASP.NET Core
/// host (via <see cref="PermissionAuthorizationHandler"/> registered through <c>Tnosc.Lib.Host</c>,
/// which references this project) and a Blazor WebAssembly client, since this project takes only the
/// <c>Microsoft.AspNetCore.Authorization</c> NuGet package — never a <c>FrameworkReference</c>.
/// </remarks>
/// <param name="permission">The permission name the caller must hold.</param>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>
    /// The claim type carrying a caller's granted permissions.
    /// </summary>
    /// <remarks>
    /// Must match the claim type <c>HttpUserContext.Permissions</c> reads, so
    /// <c>IUserContext.HasPermission(...)</c> and an endpoint's authorization policy agree about what
    /// the caller may do.
    /// </remarks>
    public const string PermissionClaimType = "permissions";

    /// <summary>
    /// Gets the permission name the caller must hold for this requirement to be satisfied.
    /// </summary>
    public string Permission { get; } = permission;
}
