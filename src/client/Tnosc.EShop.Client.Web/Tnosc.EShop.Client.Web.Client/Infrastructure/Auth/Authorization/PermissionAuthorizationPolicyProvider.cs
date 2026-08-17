// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

/// <summary>
/// Materialises an authorization policy on demand for any policy name that is a permission, so a page
/// can write <c>[Authorize(Policy = Permissions.Catalog.Write)]</c> without a matching
/// <c>AddPolicy(...)</c> call anywhere.
/// </summary>
/// <remarks>
/// The Blazor-client counterpart of <c>Tnosc.Lib.Host.Authorization.PermissionAuthorizationPolicyProvider</c>
/// — duplicated because that one lives in <c>Tnosc.Lib.Host</c>, which cannot be referenced from a
/// Blazor WebAssembly project. The inner provider is consulted first, so a policy genuinely registered
/// through <c>AddAuthorizationCore(options =&gt; options.AddPolicy(...))</c> still wins over an
/// on-demand permission policy of the same name.
/// </remarks>
/// <param name="innerProvider">
/// The provider consulted first — normally <see cref="DefaultAuthorizationPolicyProvider"/>, which
/// also supplies the default and fallback policies.
/// </param>
public sealed class PermissionAuthorizationPolicyProvider(IAuthorizationPolicyProvider innerProvider)
    : IAuthorizationPolicyProvider
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policies =
        new(comparer: StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => innerProvider.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => innerProvider.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? registered = await innerProvider.GetPolicyAsync(policyName: policyName);

        if (registered is not null)
        {
            return registered;
        }

        return _policies.GetOrAdd(key: policyName, valueFactory: static name =>
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission: name))
                .Build());
    }
}
