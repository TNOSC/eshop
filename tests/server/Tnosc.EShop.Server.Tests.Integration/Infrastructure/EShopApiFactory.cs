// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure;

/// <summary>
/// Boots the real Host over an in-memory HTTP pipeline, against the same Postgres container
/// <see cref="PostgresFixture"/> already runs, with bearer tokens validated against a symmetric test
/// key instead of a live Keycloak.
/// </summary>
/// <remarks>
/// <para>
/// Everything else is production's own wiring: the real endpoints, the real
/// <c>PermissionAuthorizationPolicyProvider</c>, and the real <c>KeycloakClaimsTransformation</c>
/// turning the token's <c>realm_access</c> claim into permissions. Only the token's <em>provenance</em>
/// is faked, which is the one part a test cannot stand up.
/// </para>
/// <para>
/// The connection string is pushed in from the fixture's already-running container rather than the
/// factory starting a second one — the schema these tests read was migrated once, by the fixture.
/// </para>
/// </remarks>
/// <param name="connectionString">The fixture's Postgres connection string.</param>
/// <param name="redisConnectionString">The fixture's Redis connection string.</param>
internal sealed class EShopApiFactory(string connectionString, string redisConnectionString) : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(key: "ConnectionStrings:eshopdb", value: connectionString);
        builder.UseSetting(key: "ConnectionStrings:cache", value: redisConnectionString);

        // The fixture owns migrations; a second migrator racing it would be the only writer here that
        // production does not have.
        builder.UseSetting(key: "Persistence:ApplyMigrationsOnStartup", value: "false");

        builder.UseEnvironment(environment: "Development");

        builder.ConfigureServices(configureServices: static services =>
        {
            // PostConfigure, so this runs after AddKeycloakJwtBearer has had its say. Authority and
            // MetadataAddress are cleared deliberately: leaving either set would send the handler to
            // service-discover a Keycloak that is not running, and the first protected request would
            // hang on metadata retrieval rather than validate the token.
            services.PostConfigure<JwtBearerOptions>(
                name: JwtBearerDefaults.AuthenticationScheme,
                configureOptions: static options =>
                {
                    options.Authority = null;
                    options.MetadataAddress = null!;
                    options.RequireHttpsMetadata = false;
                    options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration();

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            key: Encoding.UTF8.GetBytes(s: TestTokenIssuer.SigningKey)),
                        ValidateIssuer = true,
                        ValidIssuer = TestTokenIssuer.Issuer,
                        ValidateAudience = true,
                        ValidAudience = TestTokenIssuer.Audience,
                        ValidateLifetime = true,

                        // Left at its default so `sub` still maps to ClaimTypes.NameIdentifier and
                        // `email` to ClaimTypes.Email — which is what HttpUserContext reads.
                    };
                });
        });
    }
}
