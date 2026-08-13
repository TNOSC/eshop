// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Api.Identity;

/// <summary>
/// The route templates and OpenAPI tag shared by the Identity endpoints, so a path is spelled once.
/// </summary>
/// <remarks>
/// Every <c>me</c> route resolves the caller from their token rather than from a route value, which
/// is why none of them carries a customer identifier: a customer structurally cannot address another
/// customer's profile, so no ownership check exists anywhere behind these paths.
/// </remarks>
internal static class IdentityRoutes
{
    /// <summary>
    /// The OpenAPI tag every Identity endpoint is grouped under.
    /// </summary>
    public const string Tag = "Identity";

    /// <summary>
    /// The customers collection.
    /// </summary>
    public const string Customers = "/api/identity/customers";

    /// <summary>
    /// A single customer by identifier.
    /// </summary>
    public const string CustomerById = $"{Customers}/{{id:guid}}";

    /// <summary>
    /// The caller's own customer profile.
    /// </summary>
    public const string CurrentCustomer = $"{Customers}/me";

    /// <summary>
    /// The caller's own name and phone number.
    /// </summary>
    public const string CurrentCustomerProfile = $"{CurrentCustomer}/profile";

    /// <summary>
    /// The caller's own address collection.
    /// </summary>
    public const string CurrentCustomerAddresses = $"{CurrentCustomer}/addresses";

    /// <summary>
    /// A single one of the caller's own addresses.
    /// </summary>
    public const string CurrentCustomerAddressById = $"{CurrentCustomerAddresses}/{{addressId:guid}}";

    /// <summary>
    /// The default flag on one of the caller's own addresses.
    /// </summary>
    public const string CurrentCustomerDefaultAddress = $"{CurrentCustomerAddressById}/default";

    /// <summary>
    /// Where a customer changes the things Keycloak owns — email, password and password reset.
    /// Referenced from endpoint descriptions so their absence from this API reads as a decision.
    /// </summary>
    public const string AccountConsolePath = "/realms/eshop/account";

    /// <summary>
    /// A customer's name and phone number, addressed by an admin rather than the caller themselves.
    /// </summary>
    public const string CustomerProfileById = $"{CustomerById}/profile";

    /// <summary>
    /// A customer's address collection, addressed by an admin rather than the caller themselves.
    /// </summary>
    public const string CustomerAddressesById = $"{CustomerById}/addresses";

    /// <summary>
    /// A single one of a customer's addresses, addressed by an admin rather than the caller themselves.
    /// </summary>
    public const string CustomerAddressByIdAndAddressId = $"{CustomerAddressesById}/{{addressId:guid}}";

    /// <summary>
    /// The default flag on one of a customer's addresses, addressed by an admin.
    /// </summary>
    public const string CustomerDefaultAddressById = $"{CustomerAddressByIdAndAddressId}/default";

    /// <summary>
    /// Deactivating a customer's profile.
    /// </summary>
    public const string CustomerDeactivateById = $"{CustomerById}/deactivate";
}
