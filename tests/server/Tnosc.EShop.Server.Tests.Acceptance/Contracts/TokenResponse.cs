// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// Keycloak's token response, narrowed to the one field these tests use.
/// </summary>
/// <param name="AccessToken">The bearer token to present to the API.</param>
public sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
