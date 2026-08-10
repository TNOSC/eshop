// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure;

/// <summary>
/// Groups every integration test under a single xUnit collection so they share one
/// <see cref="PostgresFixture"/> — and therefore one Postgres container — instead of each test class
/// starting its own.
/// </summary>
[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
