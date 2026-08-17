// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Web.Bff;

/// <summary>The generic route patterns owned by a BFF host's login/logout/userinfo/proxy endpoints.</summary>
public static class BffRoutes
{
    /// <summary>
    /// Catch-all pattern forwarding every authenticated request under <c>/bff/api/</c> to the
    /// downstream API, preserving the remainder of the path as <c>path</c>.
    /// </summary>
    public const string ApiCatchAll = "/bff/api/{**path}";

    /// <summary>Starts the OIDC code-flow challenge against the identity provider.</summary>
    public const string Login = "/bff/login";

    /// <summary>Signs out of both the cookie session and the identity provider.</summary>
    public const string Logout = "/bff/logout";

    /// <summary>Returns the authenticated caller's identity and roles.</summary>
    public const string UserInfo = "/bff/userinfo";
}
