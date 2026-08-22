// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Agent.Host.Options;

/// <summary>
/// The Keycloak realm and audience the agent host validates access tokens against, bound from the
/// <c>"Keycloak"</c> configuration section.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the equivalent options in the storefront and MCP hosts. This host validates its own
/// <see cref="Audience"/> — <c>agent-api</c> — while the realm stamps <c>mcp-api</c> onto the same
/// token, which is what lets <c>Authentication/TokenForwarder</c> hand the caller's token straight
/// to the MCP server: it already carries the audience that server checks for.
/// </para>
/// <para>
/// There is deliberately no authority URL here. The authority tokens are validated against is
/// resolved by service discovery from the AppHost's <c>WithReference(keycloak)</c>, so nothing has
/// to be reconfigured between running locally and running deployed.
/// </para>
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
    /// Gets or sets the audience an access token must carry to be accepted by the agent host.
    /// </summary>
    [Required]
    [StringLength(maximumLength: 200, MinimumLength = 1)]
    public string Audience { get; set; } = "agent-api";

    /// <summary>
    /// Gets or sets a value indicating whether metadata retrieval requires HTTPS. Left off in
    /// development, where Keycloak is reached over the container network without a trusted certificate.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; }
}
