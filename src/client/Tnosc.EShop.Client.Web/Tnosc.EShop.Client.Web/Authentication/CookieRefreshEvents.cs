// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Tnosc.EShop.Client.Web.Authentication;

/// <summary>
/// Refreshes the access token ahead of expiry whenever the session cookie is revalidated — before the
/// request reaches the BFF proxy — using the realm's <c>refresh_token</c> grant. A failed refresh signs
/// the session out rather than letting a dead session linger.
/// </summary>
internal sealed class CookieRefreshEvents(
    IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    ILogger<CookieRefreshEvents> logger) : CookieAuthenticationEvents
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(value: 2);

    /// <summary>
    /// Checks the persisted <c>expires_at</c> token and, when it is within <see cref="RefreshMargin"/>
    /// of now, exchanges the <c>refresh_token</c> for a new access token before the request proceeds.
    /// </summary>
    /// <param name="context">The cookie validation context supplied by the cookie handler.</param>
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(argument: context);

        string? expiresAtValue = context.Properties.GetTokenValue(tokenName: "expires_at");
        if (expiresAtValue is null
            || !DateTimeOffset.TryParse(
                input: expiresAtValue,
                formatProvider: CultureInfo.InvariantCulture,
                styles: DateTimeStyles.RoundtripKind,
                result: out DateTimeOffset expiresAt))
        {
            return;
        }

        if (timeProvider.GetUtcNow() + RefreshMargin < expiresAt)
        {
            return;
        }

        string? refreshToken = context.Properties.GetTokenValue(tokenName: "refresh_token");
        if (refreshToken is null)
        {
            await RejectAsync(context: context);
            return;
        }

        OpenIdConnectOptions oidcOptions = oidcOptionsMonitor.Get(name: OpenIdConnectDefaults.AuthenticationScheme);
#pragma warning disable S8969 // ConfigurationManager is annotated nullable but AddKeycloakOpenIdConnect always assigns one; the compiler's own flow analysis (CS8602) disagrees with Sonar here.
        OpenIdConnectConfiguration configuration = await oidcOptions.ConfigurationManager!.GetConfigurationAsync(
            cancel: context.HttpContext.RequestAborted);
#pragma warning restore S8969

        using FormUrlEncodedContent content = new(nameValueCollection:
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", oidcOptions.ClientId!),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
        ]);

        using HttpClient client = httpClientFactory.CreateClient();
        using HttpResponseMessage response = await client.PostAsync(
            requestUri: configuration.TokenEndpoint,
            content: content,
            cancellationToken: context.HttpContext.RequestAborted);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(message: "Token refresh failed with status {StatusCode}.", response.StatusCode);
            await RejectAsync(context: context);
            return;
        }

        await ApplyRefreshedTokensAsync(context: context, response: response, refreshToken: refreshToken);
    }

    private async Task ApplyRefreshedTokensAsync(
        CookieValidatePrincipalContext context,
        HttpResponseMessage response,
        string refreshToken)
    {
        using JsonDocument payload = await JsonDocument.ParseAsync(
            utf8Json: await response.Content.ReadAsStreamAsync(cancellationToken: context.HttpContext.RequestAborted),
            cancellationToken: context.HttpContext.RequestAborted);

        JsonElement root = payload.RootElement;
        DateTimeOffset newExpiresAt = timeProvider.GetUtcNow()
            + TimeSpan.FromSeconds(value: root.GetProperty(propertyName: "expires_in").GetDouble());

        List<AuthenticationToken> tokens =
        [
            new AuthenticationToken
            {
                Name = "access_token",
                Value = root.GetProperty(propertyName: "access_token").GetString()!,
            },
            new AuthenticationToken
            {
                Name = "refresh_token",
                Value = root.TryGetProperty(propertyName: "refresh_token", value: out JsonElement newRefreshToken)
                    ? newRefreshToken.GetString()!
                    : refreshToken,
            },
            new AuthenticationToken
            {
                Name = "expires_at",
                Value = newExpiresAt.ToString(format: "o", formatProvider: CultureInfo.InvariantCulture),
            },
        ];

        if (root.TryGetProperty(propertyName: "id_token", value: out JsonElement idToken))
        {
            tokens.Add(item: new AuthenticationToken { Name = "id_token", Value = idToken.GetString()! });
        }

        context.Properties.StoreTokens(tokens: tokens);
        context.ShouldRenew = true;
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(scheme: CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
