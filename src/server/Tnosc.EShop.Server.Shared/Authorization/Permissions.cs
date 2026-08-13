// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Shared.Authorization;

/// <summary>
/// The fine-grained permission vocabulary endpoints name and the claims transformation grants, as
/// compile-time constants.
/// </summary>
/// <remarks>
/// <para>
/// These live in <c>Shared</c> for the same reason <c>CacheTags</c> does: the two halves of the
/// contract sit in projects that cannot see each other. <c>Server.Api</c> <em>names</em> a permission
/// on an endpoint via <c>HasPermission(...)</c>; the Host's <c>KeycloakClaimsTransformation</c>
/// <em>grants</em> it by expanding a realm role through <see cref="RolePermissions"/>. A string
/// literal on each side is two independent literals, and a typo does not fail the build — it fails at
/// runtime as a 403, which reads like a permissions bug rather than a spelling mistake.
/// </para>
/// <para>
/// Adding a permission is a code change here, never a realm edit: Keycloak owns only the coarse realm
/// roles in <see cref="Roles"/>.
/// </para>
/// </remarks>
public static class Permissions
{
    /// <summary>
    /// Permissions over the Catalog bounded context.
    /// </summary>
    public static class Catalog
    {
        /// <summary>
        /// Reading the catalogue. Not applied to the storefront read endpoints, which are deliberately
        /// anonymous — a public catalogue is a decision, not an oversight.
        /// </summary>
        public const string Read = "catalog:read";

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
        /// Reading any customer's profile by identifier. The <c>me</c> endpoints need no permission —
        /// they resolve the caller from the token, so a customer structurally cannot reach another's.
        /// </summary>
        public const string Read = "identity:read";

        /// <summary>
        /// Administering customer profiles other than the caller's own.
        /// </summary>
        public const string Write = "identity:write";
    }

    /// <summary>
    /// Permissions over the Ordering bounded context.
    /// </summary>
    /// <remarks>
    /// A customer needs none of these to order. Placing, reading, confirming and cancelling all resolve
    /// the caller from their token and scope the query or the repository lookup to them, so those
    /// endpoints carry a plain <c>RequireAuthorization()</c> and a customer structurally cannot reach
    /// another customer's order. What is left here is the back office.
    /// </remarks>
    public static class Ordering
    {
        /// <summary>
        /// Reading any customer's order, including the rolled-up back-office summary. The caller's own
        /// orders need no permission — they are reached through the <c>me</c>-shaped endpoints.
        /// </summary>
        public const string Read = "ordering:read";

        /// <summary>
        /// Despatching an order. A warehouse operation over any customer's order, which is why it is a
        /// permission rather than something scoped to the caller.
        /// </summary>
        public const string Ship = "ordering:ship";
    }
}
