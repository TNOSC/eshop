// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Bff;

/// <summary>Route patterns owned by the BFF endpoints.</summary>
internal static class BffRoutes
{
    /// <summary>
    /// Catch-all pattern forwarding every request under <c>/bff/api/</c> to the downstream API,
    /// preserving the remainder of the path as <c>path</c>.
    /// </summary>
    public const string ApiCatchAll = "/bff/api/{**path}";
}
