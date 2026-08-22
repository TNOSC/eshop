// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tnosc.EShop.Agent.Host.Authentication;
using Tnosc.EShop.Agent.Host.Options;
using Tnosc.Lib.Host.Extensions;

namespace Tnosc.EShop.Agent.Host.Extensions;

/// <summary>
/// Wires Keycloak bearer authentication into the agent host.
/// </summary>
/// <remarks>
/// <para>
/// This is the one place in this host where <c>IConfiguration</c> and <see cref="IOptions{TOptions}"/>
/// may be touched, per <c>.claude/rules/configuration-options.md</c>. The bound options are unwrapped
/// to a plain singleton immediately, so no consumer ever sees the wrapper.
/// </para>
/// <para>
/// Simpler than the MCP host's equivalent on purpose: this host speaks to browsers and HTTP clients
/// that already hold a token, not to protocol clients that have to discover an authorization server,
/// so there is no protected-resource metadata document and no second challenge scheme — a plain
/// bearer challenge is the honest answer to an unauthenticated request here.
/// </para>
/// <para>
/// No authority URL is configured: <c>AddKeycloakJwtBearer</c> composes it from the service name and
/// realm, which service discovery resolves from the AppHost's <c>WithReference(keycloak)</c>.
/// </para>
/// </remarks>
internal static class AuthenticationExtensions
{
    private const string KeycloakServiceName = "keycloak";

    /// <summary>
    /// Binds <see cref="KeycloakOptions"/> and registers JWT bearer authentication against the realm.
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

        builder.Services.AddAuthentication()
            .AddKeycloakJwtBearer(
                serviceName: KeycloakServiceName,
                realm: keycloakOptions.Realm,
                configureOptions: options =>
                {
                    options.Audience = keycloakOptions.Audience;
                    options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                });

        builder.Services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformation>();
        builder.Services.AddPermissionAuthorization();

        return builder;
    }
}
