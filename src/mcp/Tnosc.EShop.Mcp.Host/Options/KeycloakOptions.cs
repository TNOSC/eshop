// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Mcp.Host.Options;

/// <summary>
/// The Keycloak realm and audience the MCP server validates access tokens against, bound from the
/// <c>"Keycloak"</c> configuration section.
/// </summary>
/// <remarks>
/// Mirrors <c>Tnosc.EShop.Server.Host.Options.KeycloakOptions</c>. The MCP server validates its own
/// <see cref="Audience"/> — <c>mcp-api</c>. The realm's <c>mcp:tools</c> client scope stamps
/// <c>eshop-api</c> onto the same token alongside <c>mcp-api</c> (see <c>eshop-realm.json</c>), which
/// is what lets <c>Authentication/TokenForwarder</c> hand the caller's token straight to the eShop
/// API — it already carries the audience that API's own <c>KeycloakOptions.Audience</c> checks for.
/// This does mean an MCP-issued token is also valid against the storefront API and not only the MCP
/// server; a stricter split (a token minted for one server never usable against the other, RFC 8707)
/// would need Keycloak token exchange instead, which this realm's Keycloak build does not support in
/// a way that's practical to configure declaratively (see the git history around this class for what
/// was tried). It deliberately holds no *service-discovery* authority URL: the authority tokens are
/// validated against is resolved by service discovery from the AppHost's
/// <c>WithReference(keycloak)</c>. <see cref="PublicAuthorityUrl"/> is a different, narrower thing — a
/// browser-reachable address published in the protected-resource metadata so an MCP client can find
/// the authorization server to obtain a token from.
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
    /// Gets or sets the audience an access token must carry to be accepted by the MCP server.
    /// Supplied by the realm's <c>mcp-api-audience</c> protocol mapper.
    /// </summary>
    [Required]
    [StringLength(maximumLength: 200, MinimumLength = 1)]
    public string Audience { get; set; } = "mcp-api";

    /// <summary>
    /// Gets or sets a value indicating whether metadata retrieval requires HTTPS. Left off in
    /// development, where Keycloak is reached over the container network without a trusted certificate.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; }

    /// <summary>
    /// Gets or sets the browser-reachable base URL of the Keycloak server, published as the
    /// authorization server in the MCP server's protected-resource metadata document
    /// (<c>/.well-known/oauth-protected-resource</c>) so an MCP client can discover where to obtain a
    /// token.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Realm"/> and <see cref="Audience"/>, this is never used for token validation —
    /// the MCP server always resolves Keycloak by service discovery. This is the fixed host port the
    /// AppHost exposes for humans (see the comment beside <c>AddKeycloak(name: "keycloak", port: 8080)</c>
    /// in the AppHost's <c>Program.cs</c>), repeated here rather than derived, because nothing wires the
    /// two together.
    /// </remarks>
    [Required]
    [Url]
    public string PublicAuthorityUrl { get; set; } = "https://localhost:8080";
}
