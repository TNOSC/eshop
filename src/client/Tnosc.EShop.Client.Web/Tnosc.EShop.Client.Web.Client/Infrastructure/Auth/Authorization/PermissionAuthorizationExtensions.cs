// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;

/// <summary>
/// Registers permission-based authorization for a Blazor host — the Interactive Server host and the
/// WebAssembly host alike, since both resolve <see cref="IAuthorizationPolicyProvider"/> from their
/// own DI container.
/// </summary>
public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers the handler that satisfies a <see cref="PermissionRequirement"/> from the caller's
    /// claims, and the policy provider that materialises a policy for any permission name a page asks
    /// for via <c>[Authorize(Policy = ...)]</c>.
    /// </summary>
    /// <remarks>
    /// Call this after <c>AddAuthorization()</c> (Interactive Server) or <c>AddAuthorizationCore()</c>
    /// (WebAssembly) — both already register <see cref="IOptions{TOptions}"/> of
    /// <see cref="AuthorizationOptions"/>, which is all <see cref="DefaultAuthorizationPolicyProvider"/>
    /// needs. Without this call, a page's <c>Policy = Permissions.Catalog.Write</c> throws at
    /// navigation time, because nothing registers a policy under that permission's name.
    /// </remarks>
    /// <param name="services">The service collection to register authorization services on.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.TryAddEnumerable(descriptor: ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());

        // Replace, not TryAdd: AddAuthorization/AddAuthorizationCore has already registered the
        // default provider, and this one has to win — it is the whole reason a Policy name that is
        // just a permission resolves to anything.
        services.Replace(descriptor: ServiceDescriptor.Singleton<IAuthorizationPolicyProvider>(
            implementationFactory: serviceProvider => new PermissionAuthorizationPolicyProvider(
                innerProvider: new DefaultAuthorizationPolicyProvider(
                    options: serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>()))));

        return services;
    }
}
