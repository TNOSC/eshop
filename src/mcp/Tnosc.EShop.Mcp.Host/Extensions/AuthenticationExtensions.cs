// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using Tnosc.EShop.Mcp.Host.Options;

namespace Tnosc.EShop.Mcp.Host.Extensions;

/// <summary>
/// Wires Keycloak bearer authentication into the MCP host, and publishes the OAuth
/// protected-resource metadata document MCP clients use to discover how to obtain a token.
/// </summary>
/// <remarks>
/// <para>
/// This is the one place in this Host where <c>IConfiguration</c> and <see cref="IOptions{TOptions}"/>
/// may be touched, per <c>.claude/rules/configuration-options.md</c>. The bound options are unwrapped
/// to a plain singleton immediately, so no consumer ever sees the wrapper.
/// </para>
/// <para>
/// No authority URL for token validation appears anywhere: <c>AddKeycloakJwtBearer</c> composes it
/// from the service name and realm — <c>https+http://keycloak/realms/{realm}</c> — which service
/// discovery resolves from the AppHost's <c>WithReference(keycloak)</c>.
/// </para>
/// <para>
/// Unlike <c>Server.Host</c>, the challenge scheme is <c>ModelContextProtocol</c>'s own
/// <see cref="McpAuthenticationDefaults.AuthenticationScheme"/>, not the JWT bearer scheme directly.
/// That scheme is what turns a 401 into a spec-shaped <c>WWW-Authenticate</c> header pointing at
/// <c>/.well-known/oauth-protected-resource</c>, so an MCP client can discover the authorization
/// server without being told about it out of band. Authentication itself — validating the bearer
/// token — is still done by the plain JWT bearer scheme underneath.
/// </para>
/// </remarks>
internal static class AuthenticationExtensions
{
    private const string KeycloakServiceName = "keycloak";
    private static readonly string[] SupportedScopes = ["mcp:tools"];

    /// <summary>
    /// Binds <see cref="KeycloakOptions"/>, registers JWT bearer authentication against the Keycloak
    /// realm, and registers the MCP protected-resource metadata scheme in front of it.
    /// </summary>
    /// <param name="builder">The host application builder to register services on.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    internal static IHostApplicationBuilder AddKeycloakAuthentication(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        builder.Services.AddOptions<KeycloakOptions>()
            .Bind(config: builder.Configuration.GetSection(key: KeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(implementationFactory: static resolve =>
            resolve.GetRequiredService<IOptions<KeycloakOptions>>().Value);

        // Read once, here, to compose the authentication scheme. Binding again through IOptions at
        // this point would resolve before ValidateOnStart has run, so the section is read directly.
        KeycloakOptions keycloakOptions = builder.Configuration
            .GetSection(key: KeycloakOptions.SectionName)
            .Get<KeycloakOptions>() ?? new KeycloakOptions();

        string authorizationServer = $"{keycloakOptions.PublicAuthorityUrl}/realms/{keycloakOptions.Realm}";

        builder.Services.AddAuthentication(configureOptions: options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            })
            .AddKeycloakJwtBearer(
                serviceName: KeycloakServiceName,
                realm: keycloakOptions.Realm,
                configureOptions: options =>
                {
                    options.Audience = keycloakOptions.Audience;
                    options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                })
            .AddMcp(configureOptions: options =>
            {
                // "mcp:tools" is a real optional client scope on the eshop realm (see eshop-realm.json's
                // "clientScopes" / "defaultOptionalClientScopes") — it exists purely so a discovering MCP
                // client can request it during dynamic client registration without Keycloak's "Allowed
                // Client Scopes" policy rejecting the registration. This server still gates access by
                // audience alone (see KeycloakOptions); nothing here reads a scope claim off the token.
                options.ResourceMetadata = new ProtectedResourceMetadata
                {
                    AuthorizationServers = { authorizationServer },
                    ScopesSupported = SupportedScopes,
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
