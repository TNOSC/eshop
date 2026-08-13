// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Server.Host.Options;

/// <summary>
/// The Keycloak realm and audience the API validates access tokens against, bound from the
/// <c>"Keycloak"</c> configuration section.
/// </summary>
/// <remarks>
/// Deliberately holds no *service-discovery* authority URL. The authority the API validates tokens
/// against is resolved by service discovery from the AppHost's <c>WithReference(keycloak)</c>, so that
/// address is never hardcoded or configured here. <see cref="PublicAuthorityUrl"/> is a different,
/// narrower thing — a browser-reachable address needed only to drive the Scalar UI's OAuth2 login.
/// </remarks>
public sealed class KeycloakOptions
{
    /// <summary>The configuration section this class binds to.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Gets or sets the Keycloak realm issuing the tokens. Must match the realm name in
    /// <c>aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json</c>.
    /// </summary>
    [Required]
    [StringLength(maximumLength: 100, MinimumLength = 1)]
    public string Realm { get; set; } = "eshop";

    /// <summary>
    /// Gets or sets the audience an access token must carry to be accepted. Supplied by the realm's
    /// hardcoded-audience protocol mapper.
    /// </summary>
    [Required]
    [StringLength(maximumLength: 200, MinimumLength = 1)]
    public string Audience { get; set; } = "eshop-api";

    /// <summary>
    /// Gets or sets a value indicating whether metadata retrieval requires HTTPS. Left off in
    /// development, where Keycloak is reached over the container network without a trusted certificate.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; }

    /// <summary>
    /// Gets or sets the browser-reachable base URL of the Keycloak server, used only to wire the
    /// Scalar UI's OAuth2 "Authorize" button in Development.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Realm"/> and <see cref="Audience"/>, this is never used for token validation —
    /// the API always resolves Keycloak by service discovery, which is a container-network address a
    /// browser cannot reach. This is the fixed host port the AppHost exposes for humans (see the
    /// comment beside <c>AddKeycloak(name: "keycloak", port: 8080)</c> in the AppHost's
    /// <c>Program.cs</c>), repeated here rather than derived, because nothing wires the two together.
    /// Scheme is deliberately <c>https</c>: Aspire's <c>AddKeycloak</c> fronts the fixed host port with
    /// a self-signed dev certificate, not plain HTTP — a browser hitting <c>http://</c> on this port
    /// gets a connection reset, not a redirect, since Keycloak never had an HTTP listener there.
    /// </remarks>
    [Required]
    [Url]
    public string PublicAuthorityUrl { get; set; } = "https://localhost:8080";
}
