// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tnosc.EShop.Client.Web.Authentication;
using Tnosc.EShop.Client.Web.Options;
using Tnosc.Lib.Web.Bff;

namespace Tnosc.EShop.Client.Web.Extensions;

/// <summary>
/// Wires the BFF's cookie session and Keycloak OIDC code-flow challenge into the host.
/// </summary>
internal static class WebAuthenticationExtensions
{
    private const string KeycloakServiceName = "keycloak";

    /// <summary>
    /// Binds <see cref="OidcOptions"/>, registers the cookie scheme and the Keycloak OIDC challenge
    /// scheme, and wires token refresh (<see cref="CookieRefreshEvents"/>) and realm-role claim
    /// expansion (<see cref="KeycloakRoleClaimsTransformation"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one place in this host where <c>IConfiguration</c> and
    /// <see cref="IOptions{TOptions}"/> may be touched, per
    /// <c>.claude/rules/configuration-options.md</c>. The bound options are unwrapped to a plain
    /// singleton immediately, so no consumer ever sees the wrapper.
    /// </para>
    /// <para>
    /// In Development the authority is pinned to the fixed host port the AppHost exposes for Keycloak
    /// rather than resolved by service discovery: the browser is redirected here for the login page,
    /// and a browser cannot resolve a container-network address. <c>KC_HOSTNAME=localhost</c> /
    /// <c>KC_HOSTNAME_PORT=8080</c> on the Keycloak resource make this the same issuer the API's
    /// bearer validation accepts, so tokens minted here still pass server-side validation.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder to register services on.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    public static IHostApplicationBuilder AddEShopBffAuthentication(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.Services.AddOptions<OidcOptions>()
            .Bind(config: builder.Configuration.GetSection(key: OidcOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(implementationFactory: static resolve =>
            resolve.GetRequiredService<IOptions<OidcOptions>>().Value);

        // Read once, here, to compose the authentication scheme — binding again through IOptions at
        // this point would resolve before ValidateOnStart has run, so the section is read directly.
        OidcOptions oidcOptions = builder.Configuration
            .GetSection(key: OidcOptions.SectionName)
            .Get<OidcOptions>() ?? new OidcOptions();

        builder.Services.TryAddSingleton(instance: TimeProvider.System);
        builder.Services.AddTransient<CookieRefreshEvents>();

        bool isDevelopment = builder.Environment.IsDevelopment();

        IHttpClientBuilder keycloakHttpClientBuilder = builder.Services.AddHttpClient(name: CookieRefreshEvents.HttpClientName);
        if (isDevelopment)
        {
            keycloakHttpClientBuilder.ConfigurePrimaryHttpMessageHandler(
                configureHandler: static () => KeycloakDevelopmentCertificateHandler.Create());
        }

        builder.Services.AddAuthentication(configureOptions: options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(configureOptions: ConfigureCookie)
            .AddKeycloakOpenIdConnect(
                serviceName: KeycloakServiceName,
                realm: oidcOptions.Realm,
                configureOptions: options => ConfigureOpenIdConnect(
                    options: options,
                    oidcOptions: oidcOptions,
                    isDevelopment: isDevelopment));

        return builder;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = "eshop.bff";
        options.Cookie.HttpOnly = true;
        // Lax, NOT Strict — the OIDC callback lands on this cookie via a cross-site GET from
        // Keycloak; Strict would drop the cookie on that return leg and the login would loop.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(value: 8);
        options.SlidingExpiration = false; // refresh tokens drive renewal, not sliding cookies
        options.EventsType = typeof(CookieRefreshEvents);
    }

    private static void ConfigureOpenIdConnect(
        OpenIdConnectOptions options,
        OidcOptions oidcOptions,
        bool isDevelopment)
    {
        options.ClientId = oidcOptions.ClientId;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true; // eshop-web is a PUBLIC client — there is no secret
        options.SaveTokens = true; // required by ServerAccessTokenHandler and the proxy
        options.MapInboundClaims = false;
        options.Scope.Clear();
        options.Scope.Add(item: "openid");
        options.Scope.Add(item: "profile");
        options.Scope.Add(item: "email");
        options.Scope.Add(item: "offline_access"); // required by the refresh flow
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
        options.Events.OnTokenValidated = static async context =>
        {
            await KeycloakRoleClaimsTransformation.OnTokenValidatedAsync(context: context);
            await CustomerProvisioningEvents.ProvisionAsync(context: context);
        };

        if (isDevelopment)
        {
            options.Authority = $"https://localhost:8080/realms/{oidcOptions.Realm}";
            // AddKeycloak provisions an ephemeral, per-run self-signed HTTPS certificate for the
            // container's published endpoint (see aspire.dev.internal / KC_HTTPS_CERTIFICATE_FILE on
            // the Keycloak resource) — the host OS never trusts it. Discovery and the code exchange
            // both go through this handler, so this is the one place that needs to accept it.
            options.BackchannelHttpHandler = KeycloakDevelopmentCertificateHandler.Create();
        }
    }
}
