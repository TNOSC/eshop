// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

/// <summary>
/// An authorization requirement satisfied by the caller holding a named permission.
/// </summary>
/// <remarks>
/// A deliberate duplicate of <c>Tnosc.Lib.Host.Authorization.PermissionRequirement</c> — that type
/// lives in <c>Tnosc.Lib.Host</c>, which takes a <c>FrameworkReference</c> to
/// <c>Microsoft.AspNetCore.App</c> and so cannot be referenced from the Blazor WebAssembly client.
/// <see cref="PermissionClaimType"/> matches the server's constant so the same claim type flows end to
/// end: <c>KeycloakRoleClaimsTransformation</c> (server host OIDC event) writes it,
/// <c>PersistingRevalidatingAuthenticationStateProvider</c> persists it, and
/// <c>PersistentAuthenticationStateProvider</c> rebuilds it on the WASM side.
/// </remarks>
/// <param name="permission">The permission name the caller must hold.</param>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>
    /// The claim type carrying a caller's granted permissions.
    /// </summary>
    public const string PermissionClaimType = "permissions";

    /// <summary>
    /// Gets the permission name the caller must hold for this requirement to be satisfied.
    /// </summary>
    public string Permission { get; } = permission;
}
