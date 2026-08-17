// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

/// <summary>
/// The fine-grained permission vocabulary Blazor pages name via <c>[Authorize(Policy = ...)]</c> or
/// <c>&lt;AuthorizeView Policy="..."&gt;</c>, and <see cref="RolePermissions"/> grants.
/// </summary>
/// <remarks>
/// A deliberate duplicate of <c>Tnosc.EShop.Server.Shared.Authorization.Permissions</c> — see
/// <see cref="Roles"/> for why this project does not reference <c>Server.Shared</c> to reuse it
/// instead. Only the values an admin page actually checks are mirrored here; add more as the back
/// office grows rather than copying the whole server vocabulary speculatively.
/// </remarks>
public static class Permissions
{
    /// <summary>
    /// Permissions over the Catalog bounded context.
    /// </summary>
    public static class Catalog
    {
        /// <summary>
        /// Creating products and changing their price or stock level.
        /// </summary>
        public const string Write = "catalog:write";
    }

    /// <summary>
    /// Permissions over the Identity bounded context.
    /// </summary>
    public static class Identity
    {
        /// <summary>
        /// Administering customer profiles other than the caller's own.
        /// </summary>
        public const string Write = "identity:write";
    }
}
