// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth;

/// <summary>
/// Rebuilds the <see cref="AuthenticationState"/> on the WASM side from the <see cref="UserInfo"/>
/// snapshot the server persisted, instead of re-authenticating against Keycloak. Fixed for the
/// lifetime of this instance — WASM never revalidates it, since the cookie session lives on the server.
/// </summary>
/// <remarks>
/// Losing the <see cref="ClaimTypes.Role"/> or permission claims here is exactly what makes
/// <c>&lt;AuthorizeView Roles="admin"&gt;</c> and <c>[Authorize(Policy = Permissions.Catalog.Write)]</c>
/// pass during prerender and then fail the moment the app becomes interactive — see
/// <c>PersistingRevalidatingAuthenticationStateProvider</c> on the server side, which is what persists
/// <see cref="UserInfo.Roles"/> and <see cref="UserInfo.Permissions"/> in the first place.
/// </remarks>
internal sealed class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> UnauthenticatedState =
        Task.FromResult(result: new AuthenticationState(user: new ClaimsPrincipal(identity: new ClaimsIdentity())));

    private readonly Task<AuthenticationState> authenticationStateTask;

    public PersistentAuthenticationStateProvider(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson(key: nameof(UserInfo), instance: out UserInfo? userInfo) || userInfo is null)
        {
            authenticationStateTask = UnauthenticatedState;
            return;
        }

        ClaimsIdentity identity = new(
            claims:
            [
                new Claim(type: ClaimTypes.NameIdentifier, value: userInfo.UserId),
                new Claim(type: ClaimTypes.Name, value: userInfo.Name),
                .. userInfo.Roles.Select(selector: static role => new Claim(type: ClaimTypes.Role, value: role)),
                .. userInfo.Permissions.Select(selector: static permission =>
                    new Claim(type: PermissionRequirement.PermissionClaimType, value: permission)),
            ],
            authenticationType: "Persisted");

        authenticationStateTask = Task.FromResult(
            result: new AuthenticationState(user: new ClaimsPrincipal(identity: identity)));
    }

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => authenticationStateTask;
}
