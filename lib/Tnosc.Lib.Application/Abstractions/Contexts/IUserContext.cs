// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.Lib.Application.Abstractions.Contexts;

/// <summary>
/// Provides information about the current caller, independent of the underlying transport.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets a value indicating whether the current caller is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the identifier of the current user, or <see langword="null"/> when unauthenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the email address of the current user, or <see langword="null"/> when unavailable.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the roles assigned to the current user. Empty when unauthenticated.
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Gets the permissions granted to the current user. Empty when unauthenticated.
    /// </summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>
    /// Determines whether the current user belongs to the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns><see langword="true"/> if the user belongs to the role; otherwise, <see langword="false"/>.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Determines whether the current user has been granted the specified permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns><see langword="true"/> if the user has the permission; otherwise, <see langword="false"/>.</returns>
    bool HasPermission(string permission);
}
