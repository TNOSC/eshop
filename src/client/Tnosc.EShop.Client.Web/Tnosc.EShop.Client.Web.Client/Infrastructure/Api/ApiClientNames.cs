// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Api;

/// <summary>
/// The logical names the typed API clients are registered under, so their outbound requests are
/// identifiable in logging and telemetry regardless of which host resolves them.
/// </summary>
public static class ApiClientNames
{
    /// <summary>The Catalog typed client.</summary>
    public const string Catalog = "eshop-catalog";

    /// <summary>The BFF's downstream forwarder client, targeting <c>eshop-host</c> directly.</summary>
    public const string Downstream = "eshop-downstream";
}
