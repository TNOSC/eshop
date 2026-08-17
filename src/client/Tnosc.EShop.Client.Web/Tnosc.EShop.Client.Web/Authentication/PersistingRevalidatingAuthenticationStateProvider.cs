// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

namespace Tnosc.EShop.Client.Web.Authentication;

/// <summary>
/// Revalidates the interactive circuit's authentication state every <see cref="RevalidationInterval"/>
/// and, on every persist, snapshots the caller's identity, realm roles and permissions into a
/// <see cref="UserInfo"/> written to <see cref="PersistentComponentState"/> — the state
/// <c>PersistentAuthenticationStateProvider</c> reads back once WASM takes over. Persisting the roles
/// and permissions is what keeps <c>&lt;AuthorizeView Roles="admin"&gt;</c> and
/// <c>[Authorize(Policy = Permissions.Catalog.Write)]</c> correct after the interactive switch; miss
/// it and the admin nav appears during prerender, then vanishes the moment the app becomes interactive.
/// </summary>
/// <remarks>
/// Deliberately a classic constructor, not a primary one: subscribing to
/// <see cref="AuthenticationStateProvider.AuthenticationStateChanged"/> and registering the persisting
/// callback are side effects a primary-constructor field initializer cannot express.
/// </remarks>
internal sealed class PersistingRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly PersistentComponentState persistentComponentState;
    private readonly PersistingComponentStateSubscription subscription;
    private Task<AuthenticationState>? authenticationStateTask;

    public PersistingRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        PersistentComponentState persistentComponentState)
        : base(loggerFactory: loggerFactory)
    {
        this.persistentComponentState = persistentComponentState;

        AuthenticationStateChanged += OnAuthenticationStateChanged;
        subscription = persistentComponentState.RegisterOnPersisting(
            callback: OnPersistingAsync,
            renderMode: RenderMode.InteractiveWebAssembly);
    }

    /// <inheritdoc />
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(value: 30);

    /// <inheritdoc />
    /// <remarks>
    /// The cookie's own validity — expiry and refresh — is enforced per request by
    /// <see cref="CookieRefreshEvents"/>, upstream of any circuit this provider runs in. There is
    /// nothing further to check for a Keycloak-backed identity, so revalidation always accepts the
    /// ticket the circuit started with.
    /// </remarks>
    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken) =>
        Task.FromResult(result: true);

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) => authenticationStateTask = task;

    private async Task OnPersistingAsync()
    {
        if (authenticationStateTask is null)
        {
            throw new InvalidOperationException(message: "Authentication state was not set before persisting.");
        }

        AuthenticationState authenticationState = await authenticationStateTask;
        ClaimsPrincipal principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return;
        }

        string? userId = principal.FindFirst(type: ClaimTypes.NameIdentifier)?.Value;
        string? name = principal.Identity.Name;

        if (userId is null || name is null)
        {
            return;
        }

        UserInfo userInfo = new(
            UserId: userId,
            Name: name,
            Roles: [.. principal.FindAll(type: ClaimTypes.Role).Select(selector: static claim => claim.Value)],
            Permissions:
            [
                .. principal.FindAll(type: PermissionRequirement.PermissionClaimType)
                    .Select(selector: static claim => claim.Value),
            ]);

        persistentComponentState.PersistAsJson(key: nameof(UserInfo), instance: userInfo);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
        base.Dispose(disposing: disposing);
    }
}
