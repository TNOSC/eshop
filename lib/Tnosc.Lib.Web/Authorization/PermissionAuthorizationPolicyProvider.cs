// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Tnosc.Lib.Web.Authorization;

/// <summary>
/// Materialises an authorization policy on demand for any policy name that is a permission, so
/// permissions never have to be registered one by one at startup.
/// </summary>
/// <remarks>
/// <para>
/// On an ASP.NET Core host this is the load-bearing piece behind
/// <c>ApiEndpointExtensions.HasPermission(permission)</c>, which is just
/// <c>RequireAuthorization(policyNames: permission)</c>; on a Blazor host it backs
/// <c>[Authorize(Policy = ...)]</c>. Nothing registers a policy per permission, so without this
/// provider such a call would throw at request/navigation time with "The AuthorizationPolicy named
/// ... was not found". Here an unrecognised name is turned into
/// <c>RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(name))</c> and memoised.
/// </para>
/// <para>
/// Requiring an authenticated user as well as the permission is what yields the two distinct
/// outcomes callers expect for free: <c>401</c> when no (or an invalid) token was supplied, and
/// <c>403</c> when the token is good but does not carry the permission.
/// </para>
/// <para>
/// The inner provider is consulted first, so a policy genuinely registered through
/// <c>AddAuthorization(options =&gt; options.AddPolicy(...))</c> still wins over an
/// on-demand permission policy of the same name.
/// </para>
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
