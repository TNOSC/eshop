// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Web.Bff;

/// <summary>
/// The minimal snapshot of the signed-in caller a BFF-pattern Blazor host persists from the server
/// render into <c>PersistentComponentState</c>, so the WebAssembly runtime can rebuild an equivalent
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> without a round trip to the identity provider.
/// </summary>
/// <param name="UserId">The caller's subject id.</param>
/// <param name="Name">The caller's display name.</param>
/// <param name="Roles">The caller's roles.</param>
/// <param name="Permissions">
/// The fine-grained permissions the caller's roles grant, expanded server-side by the host's own
/// claims transformation. Persisted separately from <see cref="Roles"/> so
/// <c>[Authorize(Policy = "some:permission")]</c> keeps working after the interactive switch to
/// WebAssembly, the same way persisting <see cref="Roles"/> keeps
/// <c>&lt;AuthorizeView Roles="admin"&gt;</c> working.
/// </param>
public sealed record UserInfo(string UserId, string Name, string[] Roles, string[] Permissions);
