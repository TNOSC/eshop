// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Client.Web.Options;

/// <summary>
/// The Keycloak realm and public client the BFF's OIDC code flow authenticates against, bound from
/// the <c>"Oidc"</c> configuration section.
/// </summary>
/// <remarks>
/// No client secret appears anywhere: <c>eshop-web</c> is a public client, and the code flow is
/// secured with PKCE instead.
/// </remarks>
public sealed class OidcOptions
{
    /// <summary>The configuration section this class binds to.</summary>
    public const string SectionName = "Oidc";

    /// <summary>
    /// Gets or sets the Keycloak realm to authenticate against. Must match the realm name in
    /// <c>aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json</c>.
    /// </summary>
    [Required]
    [StringLength(maximumLength: 100, MinimumLength = 1)]
    public string Realm { get; set; } = "eshop";

    /// <summary>
    /// Gets or sets the public OIDC client id registered for this web app in the realm.
    /// </summary>
    [Required]
    [StringLength(maximumLength: 100, MinimumLength = 1)]
    public string ClientId { get; set; } = "eshop-web";
}
