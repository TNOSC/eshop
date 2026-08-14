// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Tests.Acceptance;

/// <summary>
/// Groups every acceptance test onto one booted application. Collections also serialize the tests
/// inside them, which matters here: the journeys share a Keycloak account, and therefore a basket.
/// </summary>
[CollectionDefinition(name: nameof(AppHostCollection))]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>
{
    // Marker type. xUnit never constructs it.
}
