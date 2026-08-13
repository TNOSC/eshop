// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tnosc.Lib.Host.Authorization;

namespace Tnosc.Lib.Host.Extensions;

/// <summary>
/// Provides extension methods to register permission-based authorization.
/// </summary>
public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers permission-based authorization: the handler that satisfies a
    /// <see cref="PermissionRequirement"/> from the caller's claims, and the policy provider that
    /// materialises a policy for any permission name an endpoint asks for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this alongside <c>AddAuthentication(...)</c>. Without it, every endpoint built with
    /// <c>HasPermission(...)</c> throws at request time, because nothing registers a policy under
    /// that permission's name.
    /// </para>
    /// <para>
    /// <see cref="IOptions{TOptions}"/> is touched here and only here — inside a composition-root
    /// <c>AddXxx</c> method, which is the one sanctioned site for it. It is unwrapped immediately to
    /// construct the framework's <see cref="DefaultAuthorizationPolicyProvider"/>, so
    /// <see cref="PermissionAuthorizationPolicyProvider"/> itself takes a plain
    /// <see cref="IAuthorizationPolicyProvider"/> and stays constructible — and unit-testable —
    /// without any options wrapper.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to register authorization services on.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.TryAddEnumerable(descriptor: ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());

        // Replace, not TryAdd: AddAuthorization has already registered the default provider, and this
        // one has to win — it is the whole reason HasPermission(...) resolves to anything.
        services.Replace(descriptor: ServiceDescriptor.Singleton<IAuthorizationPolicyProvider>(
            implementationFactory: serviceProvider => new PermissionAuthorizationPolicyProvider(
                innerProvider: new DefaultAuthorizationPolicyProvider(
                    options: serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>()))));

        return services;
    }
}
