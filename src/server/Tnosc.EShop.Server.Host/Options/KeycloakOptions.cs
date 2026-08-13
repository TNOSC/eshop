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
/// Deliberately holds no authority URL. The authority is resolved by service discovery from the
/// AppHost's <c>WithReference(keycloak)</c>, so no environment's Keycloak address is ever hardcoded
/// or configured here — only the realm and audience, which are the same everywhere.
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
}
