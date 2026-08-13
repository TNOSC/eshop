// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tnosc.EShop.Server.Host.Authentication;
using Tnosc.EShop.Server.Host.Options;
using Tnosc.Lib.Host.Extensions;

namespace Tnosc.EShop.Server.Host.Extensions;

/// <summary>
/// Wires Keycloak bearer authentication and permission-based authorization into the host.
/// </summary>
public static class AuthenticationExtensions
{
    private const string KeycloakServiceName = "keycloak";

    /// <summary>
    /// Binds <see cref="KeycloakOptions"/>, registers JWT bearer authentication against the Keycloak
    /// realm, adds the claims transformation that turns realm roles into permissions, and registers
    /// permission-based authorization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one place in the Host where <c>IConfiguration</c> and <see cref="IOptions{TOptions}"/>
    /// may be touched, per <c>.claude/rules/configuration-options.md</c> — a rule
    /// <c>ConfigurationTests</c> enforces against this assembly too. The bound options are unwrapped
    /// to a plain singleton immediately, so no consumer ever sees the wrapper.
    /// </para>
    /// <para>
    /// No authority URL appears anywhere: <c>AddKeycloakJwtBearer</c> composes it from the service
    /// name and realm — <c>https+http://keycloak/realms/{realm}</c> — which service discovery resolves
    /// from the AppHost's <c>WithReference(keycloak)</c>.
    /// </para>
    /// <para>
    /// <c>MapInboundClaims</c> is deliberately left at its default, so <c>sub</c> maps to
    /// <c>ClaimTypes.NameIdentifier</c> and <c>email</c> to <c>ClaimTypes.Email</c> — exactly what
    /// <c>HttpUserContext</c> reads.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder to register services on.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    public static IHostApplicationBuilder AddKeycloakAuthentication(this IHostApplicationBuilder builder)
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

        builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
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
