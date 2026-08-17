// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net.Http;

namespace Tnosc.Lib.Web.Bff;

/// <summary>
/// Builds an <see cref="HttpClientHandler"/> that trusts the ephemeral, per-run self-signed
/// certificate an Aspire-hosted Keycloak container publishes for Development — the host OS never
/// trusts it, since it is regenerated on every run. Share the same instance between every backchannel
/// call to that Keycloak endpoint (OIDC discovery, the authorization-code exchange, and any
/// <c>refresh_token</c> grant), so they all trust the one certificate consistently.
/// </summary>
public static class KeycloakDevelopmentCertificateHandler
{
    /// <summary>
    /// Creates a handler that accepts any server certificate. Development-only: never call this
    /// outside a <c>Development</c> environment check.
    /// </summary>
#pragma warning disable MA0039, S4830 // Development-only: Aspire provisions an ephemeral, per-run self-signed certificate for the Keycloak container endpoint that the OS never trusts; there is no chain to validate against.
    public static HttpClientHandler Create() =>
        new()
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
#pragma warning restore MA0039, S4830
}
