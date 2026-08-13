// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Shouldly;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Host.Authorization;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.Authorization;

/// <summary>
/// The piece that makes <c>HasPermission(...)</c> work at all: nothing registers a policy per
/// permission, so an unrecognised policy name has to be materialised on demand.
/// </summary>
public sealed class PermissionAuthorizationPolicyProviderTests
{
    private readonly PermissionAuthorizationPolicyProvider _provider = new(
        innerProvider: new DefaultAuthorizationPolicyProvider(options: Options.Create(new AuthorizationOptions())));

    [Fact]
    public async Task GetPolicyAsync_Should_MaterializeAPolicy_ForAPermissionNobodyRegistered()
    {
        // Act
        AuthorizationPolicy? policy = await _provider.GetPolicyAsync(policyName: Permissions.Catalog.Write);

        // Assert
        policy.ShouldNotBeNull(customMessage: "Without this, every HasPermission endpoint throws at request time.");
    }

    // Requiring an authenticated user alongside the permission is what yields 401-unauthenticated
    // versus 403-authenticated-but-unpermitted, rather than collapsing both into one status.
    [Fact]
    public async Task GetPolicyAsync_Should_RequireAnAuthenticatedUser_AsWellAsThePermission()
    {
        // Act
        AuthorizationPolicy policy = (await _provider.GetPolicyAsync(policyName: Permissions.Catalog.Write))!;

        // Assert
        policy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();

        PermissionRequirement requirement = policy.Requirements.OfType<PermissionRequirement>().ShouldHaveSingleItem();
        requirement.Permission.ShouldBe(expected: Permissions.Catalog.Write);
    }

    [Fact]
    public async Task GetPolicyAsync_Should_MemoizeThePolicy_AcrossCalls()
    {
        // Act
        AuthorizationPolicy? first = await _provider.GetPolicyAsync(policyName: Permissions.Identity.Read);
        AuthorizationPolicy? second = await _provider.GetPolicyAsync(policyName: Permissions.Identity.Read);

        // Assert
        second.ShouldBeSameAs(expected: first);
    }

    [Fact]
    public async Task GetPolicyAsync_Should_BuildADistinctPolicy_PerPermissionName()
    {
        // Act
        AuthorizationPolicy read = (await _provider.GetPolicyAsync(policyName: Permissions.Catalog.Read))!;
        AuthorizationPolicy write = (await _provider.GetPolicyAsync(policyName: Permissions.Catalog.Write))!;

        // Assert
        read.Requirements.OfType<PermissionRequirement>().Single().Permission.ShouldBe(expected: Permissions.Catalog.Read);
        write.Requirements.OfType<PermissionRequirement>().Single().Permission.ShouldBe(expected: Permissions.Catalog.Write);
    }

    [Fact]
    public async Task GetPolicyAsync_Should_PreferAnExplicitlyRegisteredPolicy_OverAMaterializedOne()
    {
        // Arrange
        var options = new AuthorizationOptions();
        options.AddPolicy(name: Permissions.Catalog.Write, configurePolicy: builder => builder.RequireAssertion(handler: static _ => true));

        var provider = new PermissionAuthorizationPolicyProvider(
            innerProvider: new DefaultAuthorizationPolicyProvider(options: Options.Create(options)));

        // Act
        AuthorizationPolicy policy = (await provider.GetPolicyAsync(policyName: Permissions.Catalog.Write))!;

        // Assert
        policy.Requirements.OfType<PermissionRequirement>().ShouldBeEmpty(
            customMessage: "A hand-registered policy of the same name must win over the on-demand one.");
    }

    [Fact]
    public async Task GetDefaultPolicyAsync_Should_DelegateToTheInnerProvider()
    {
        // Act
        AuthorizationPolicy policy = await _provider.GetDefaultPolicyAsync();

        // Assert
        policy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();
    }
}
