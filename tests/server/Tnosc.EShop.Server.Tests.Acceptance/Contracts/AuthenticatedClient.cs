// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net.Http;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// An API client carrying a bearer token, together with that token's subject.
/// </summary>
/// <param name="Client">The authenticated HTTP client.</param>
/// <param name="Subject">The token's <c>sub</c> claim — the caller's identifier throughout the API.</param>
public sealed record AuthenticatedClient(HttpClient Client, string Subject);
