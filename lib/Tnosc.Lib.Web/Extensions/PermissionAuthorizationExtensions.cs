// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tnosc.Lib.Web.Authorization;

namespace Tnosc.Lib.Web.Extensions;

/// <summary>
/// Registers permission-based authorization — on an ASP.NET Core host or a Blazor host (Interactive
/// Server or WebAssembly) alike, since all three resolve <see cref="IAuthorizationPolicyProvider"/>
/// from their own DI container the same way.
/// </summary>
public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers the handler that satisfies a <see cref="PermissionRequirement"/> from the caller's
    /// claims, and the policy provider that materialises a policy for any permission name an endpoint
    /// or a page asks for.
    /// </summary>
    /// <remarks>
    /// Call this after <c>AddAuthorization()</c> (ASP.NET Core / Interactive Server) or
    /// <c>AddAuthorizationCore()</c> (WebAssembly) — both already register
    /// <see cref="IOptions{TOptions}"/> of <see cref="AuthorizationOptions"/>, which is all
    /// <see cref="DefaultAuthorizationPolicyProvider"/> needs. Without this call, an endpoint's
    /// <c>HasPermission(...)</c> or a page's <c>Policy = Permissions.Catalog.Write</c> throws at
    /// request/navigation time, because nothing registers a policy under that permission's name.
    /// </remarks>
    /// <param name="services">The service collection to register authorization services on.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.TryAddEnumerable(descriptor: ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());

        // Replace, not TryAdd: AddAuthorization/AddAuthorizationCore has already registered the
        // default provider, and this one has to win — it is the whole reason HasPermission(...) /
        // a permission-named Policy resolves to anything.
        services.Replace(descriptor: ServiceDescriptor.Singleton<IAuthorizationPolicyProvider>(
            implementationFactory: serviceProvider => new PermissionAuthorizationPolicyProvider(
                innerProvider: new DefaultAuthorizationPolicyProvider(
                    options: serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>()))));

        return services;
    }
}
