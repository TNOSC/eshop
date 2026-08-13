// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Tnosc.EShop.Server.Host.Authentication;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Host.Authorization;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Authorization;

/// <summary>
/// The Keycloak-specific half of the authorization model: <c>realm_access</c> roles become
/// <see cref="ClaimTypes.Role"/> claims, and each role expands through <see cref="RolePermissions"/>
/// into permission claims.
/// </summary>
public sealed class KeycloakClaimsTransformationTests
{
    private readonly KeycloakClaimsTransformation _transformation = new();

    [Fact]
    public async Task TransformAsync_Should_AddARoleClaim_PerRealmRole()
    {
        // Arrange
        ClaimsPrincipal principal = PrincipalWithRealmRoles(Roles.Admin, Roles.Customer);

        // Act
        ClaimsPrincipal transformed = await _transformation.TransformAsync(principal: principal);

        // Assert
        RolesOf(principal: transformed).ShouldBe(expected: [Roles.Admin, Roles.Customer], ignoreOrder: true);
    }

    [Fact]
    public async Task TransformAsync_Should_ExpandTheAdminRole_IntoItsPermissions()
    {
        // Arrange
        ClaimsPrincipal principal = PrincipalWithRealmRoles(Roles.Admin);

        // Act
        ClaimsPrincipal transformed = await _transformation.TransformAsync(principal: principal);

        // Assert
        PermissionsOf(principal: transformed).ShouldBe(
            expected: [.. RolePermissions.For(role: Roles.Admin)],
            ignoreOrder: true);
    }

    // The 403 half of the story: a customer token reaches the catalogue but not its write endpoints.
    [Fact]
    public async Task TransformAsync_Should_GrantCatalogRead_ButNotCatalogWrite_ForTheCustomerRole()
    {
        // Arrange
        ClaimsPrincipal principal = PrincipalWithRealmRoles(Roles.Customer);

        // Act
        ClaimsPrincipal transformed = await _transformation.TransformAsync(principal: principal);

        // Assert
        IReadOnlyCollection<string> permissions = PermissionsOf(principal: transformed);
        permissions.ShouldContain(expected: Permissions.Catalog.Read, comparer: StringComparer.Ordinal);
        permissions.ShouldNotContain(expected: Permissions.Catalog.Write, comparer: StringComparer.Ordinal);
        permissions.ShouldNotContain(expected: Permissions.Identity.Read, comparer: StringComparer.Ordinal);
    }

    [Fact]
    public async Task TransformAsync_Should_GrantNothing_When_TheRoleIsUnknown()
    {
        // Arrange
        ClaimsPrincipal principal = PrincipalWithRealmRoles("warehouse-robot");

        // Act
        ClaimsPrincipal transformed = await _transformation.TransformAsync(principal: principal);

        // Assert
        RolesOf(principal: transformed).ShouldBe(expected: ["warehouse-robot"], customMessage: "An unknown realm role is still a role.");
        PermissionsOf(principal: transformed).ShouldBeEmpty(customMessage: "An unknown realm role must grant no permission at all.");
    }

    // IClaimsTransformation can run more than once per request, so every AddClaim is guarded. Without
    // that guard this test would see each role and permission twice.
    [Fact]
    public async Task TransformAsync_Should_AddNothingFurther_When_ItRunsASecondTime()
    {
        // Arrange
        ClaimsPrincipal principal = PrincipalWithRealmRoles(Roles.Admin);

        // Act
        ClaimsPrincipal once = await _transformation.TransformAsync(principal: principal);
        int rolesAfterFirstRun = RolesOf(principal: once).Count;
        int permissionsAfterFirstRun = PermissionsOf(principal: once).Count;

        ClaimsPrincipal twice = await _transformation.TransformAsync(principal: once);

        // Assert
        RolesOf(principal: twice).Count.ShouldBe(expected: rolesAfterFirstRun);
        PermissionsOf(principal: twice).Count.ShouldBe(expected: permissionsAfterFirstRun);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("{}")]
    [InlineData("{\"roles\":\"not-an-array\"}")]
    public async Task TransformAsync_Should_GrantNothing_When_TheRealmAccessClaimIsUnusable(string realmAccess)
    {
        // Arrange
        var identity = new ClaimsIdentity(authenticationType: "Test");
        identity.AddClaim(claim: new Claim(type: "realm_access", value: realmAccess));
        var principal = new ClaimsPrincipal(identity: identity);

        // Act
        ClaimsPrincipal transformed = await _transformation.TransformAsync(principal: principal);

        // Assert
        RolesOf(principal: transformed).ShouldBeEmpty();
        PermissionsOf(principal: transformed).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_Should_GrantNothing_When_ThereIsNoRealmAccessClaim()
    {
        // Arrange
        var principal = new ClaimsPrincipal(identity: new ClaimsIdentity(authenticationType: "Test"));

        // Act
        ClaimsPrincipal transformed = await _transformation.TransformAsync(principal: principal);

        // Assert
        PermissionsOf(principal: transformed).ShouldBeEmpty();
    }

    private static ClaimsPrincipal PrincipalWithRealmRoles(params string[] roles)
    {
        string json = $"{{\"roles\":[{string.Join(separator: ",", values: roles.Select(selector: role => $"\"{role}\""))}]}}";

        var identity = new ClaimsIdentity(authenticationType: "Test");
        identity.AddClaim(claim: new Claim(type: "realm_access", value: json));

        return new ClaimsPrincipal(identity: identity);
    }

    private static IReadOnlyCollection<string> RolesOf(ClaimsPrincipal principal) =>
        [.. principal.FindAll(type: ClaimTypes.Role).Select(selector: claim => claim.Value)];

    private static IReadOnlyCollection<string> PermissionsOf(ClaimsPrincipal principal) =>
        [.. principal.FindAll(type: PermissionRequirement.PermissionClaimType).Select(selector: claim => claim.Value)];
}
