// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth;

/// <summary>
/// The minimal snapshot of the signed-in caller persisted from the server render into
/// <c>PersistentComponentState</c>, so the WASM runtime can rebuild an equivalent
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> without a round trip to Keycloak.
/// </summary>
/// <param name="UserId">The caller's Keycloak subject id.</param>
/// <param name="Name">The caller's display name.</param>
/// <param name="Roles">The caller's realm roles.</param>
/// <param name="Permissions">
/// The fine-grained permissions the caller's roles grant, expanded server-side by
/// <c>KeycloakRoleClaimsTransformation</c> through <c>Authorization.RolePermissions</c>. Persisted
/// separately from <see cref="Roles"/> so <c>[Authorize(Policy = Permissions.Catalog.Write)]</c> keeps
/// working after the interactive switch to WebAssembly, the same way persisting <see cref="Roles"/>
/// keeps <c>&lt;AuthorizeView Roles="admin"&gt;</c> working.
/// </param>
public sealed record UserInfo(string UserId, string Name, string[] Roles, string[] Permissions);
