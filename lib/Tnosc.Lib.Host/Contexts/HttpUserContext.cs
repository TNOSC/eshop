// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tnosc.Lib.Application.Abstractions.Contexts;

namespace Tnosc.Lib.Host.Contexts;

/// <summary>
/// An <see cref="IUserContext"/> implementation backed by the current <see cref="HttpContext"/>.
/// </summary>
/// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
internal sealed class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private const string PermissionClaimType = "permissions";

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    private ClaimsPrincipal? AuthenticatedUser => User?.Identity?.IsAuthenticated == true ? User : null;

    /// <inheritdoc />
    public bool IsAuthenticated => AuthenticatedUser is not null;

    /// <inheritdoc />
    public string? UserId => AuthenticatedUser?.FindFirst(type: ClaimTypes.NameIdentifier)?.Value
        ?? AuthenticatedUser?.FindFirst(type: "sub")?.Value;

    /// <inheritdoc />
    public string? Email => AuthenticatedUser?.FindFirst(type: ClaimTypes.Email)?.Value
        ?? AuthenticatedUser?.FindFirst(type: "email")?.Value;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        AuthenticatedUser is null ? [] : [.. AuthenticatedUser.FindAll(type: ClaimTypes.Role).Select(selector: claim => claim.Value)];

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions =>
        AuthenticatedUser is null ? [] : [.. AuthenticatedUser.FindAll(type: PermissionClaimType).Select(selector: claim => claim.Value)];

    /// <inheritdoc />
    public bool IsInRole(string role) => Roles.Contains(value: role, comparer: System.StringComparer.Ordinal);

    /// <inheritdoc />
    public bool HasPermission(string permission) => Permissions.Contains(value: permission, comparer: System.StringComparer.Ordinal);
}
